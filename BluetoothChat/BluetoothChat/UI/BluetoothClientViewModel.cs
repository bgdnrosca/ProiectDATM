using BluetoothChat.Helpers;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;
using UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding;

namespace BluetoothChat.UI
{
    public enum ClientViewType
    {
        BluetoothConnectView,
        ChatView
    }

    public class BluetoothClientViewModel : ViewModelBase
    {
        public ObservableCollection<DeviceInformation> AvailableDevices { get; private set; }
        private DeviceWatcher m_deviceWatcher = null;
        private StreamSocket m_chatSocket = null;
        private DataWriter m_chatWriter = null;
        private RfcommDeviceService m_chatService = null;
        private BluetoothDevice m_bluetoothDevice;
        private ClientViewType m_viewType;
        private TaskAdapter m_loadingTask;

        public RelayCommand<DeviceInformation> DeviceSelectedCommand { get; }

        public ICommand DisconnectCommand { get; }

        public ICommand StartSearch { get; }

        public ICommand StopSearch { get; }

        public ClientViewType ClientViewType
        {
            get { return m_viewType; }
            set { Set(nameof(ClientViewType), ref m_viewType, value); }
        }

        public TaskAdapter LoadingTask
        {
            get { return m_loadingTask; }
            set { Set(nameof(LoadingTask), ref m_loadingTask, value); }
        }

        public BluetoothDevice BluetoothDevice
        {
            get { return m_bluetoothDevice; }
            set { Set(nameof(BluetoothDevice), ref m_bluetoothDevice, value); }
        }

        public ObservableCollection<string> MessageBoxList { get; }

        public BluetoothClientViewModel()
        {
            AvailableDevices = new ObservableCollection<DeviceInformation>();
            DeviceSelectedCommand = new RelayCommand<DeviceInformation>(OnAvailableDeviceSelected);
            StartSearch = new RelayCommand(StartUnpairedDeviceWatcher);
            StopSearch = new RelayCommand(StopUnpairedDeviceWatcher);
            ClientViewType = ClientViewType.BluetoothConnectView;
            MessageBoxList = new ObservableCollection<string>();
            DisconnectCommand = new RelayCommand(Disconnect);
        }

        private void StopUnpairedDeviceWatcher()
        {
            try
            {
                m_deviceWatcher?.Stop();
                AvailableDevices.Clear();
            }
            catch (Exception ex)
            {
                //ERROR
            }
        }

        private void OnAvailableDeviceSelected(DeviceInformation device)
        {
            LoadingTask = new TaskAdapter(ConnectToSelectedDeviceAsync(device));
        }

        private async Task ConnectToSelectedDeviceAsync(DeviceInformation device)
        {
            if (device == null)
            {
                return;
            }
            try
            {
                BluetoothDevice = await BluetoothDevice.FromIdAsync(device.Id);
            }
            catch (Exception ex)
            {
                //ERROR
                return;
            }
            if (m_bluetoothDevice == null)
            {
                //ERROR
                return;
            }

            // This should return a list of uncached Bluetooth services (so if the server was not active when paired, it will still be detected by this call
            var rfcommServices = await m_bluetoothDevice.GetRfcommServicesForIdAsync(
                RfcommServiceId.FromUuid(Constants.BluetoothDatmGUID), BluetoothCacheMode.Uncached);

            if (rfcommServices.Services.Count > 0)
            {
                m_chatService = rfcommServices.Services[0];
            }
            else
            {
                //ERROR
                return;
            }

            // Do various checks of the SDP record to make sure you are talking to a device that actually supports the Bluetooth Rfcomm Chat Service
            var attributes = await m_chatService.GetSdpRawAttributesAsync();
            if (!attributes.ContainsKey(Constants.SdpServiceNameAttributeId))
            {
                //"The Chat service is not advertising the Service Name attribute (attribute id=0x100). " +
                //"Please verify that you are running the BluetoothRfcommChat server."
                return;
            }
            var attributeReader = DataReader.FromBuffer(attributes[Constants.SdpServiceNameAttributeId]);
            var attributeType = attributeReader.ReadByte();
            if (attributeType != Constants.SdpServiceNameAttributeType)
            {
                //"The Chat service is using an unexpected format for the Service Name attribute. " +
                //"Please verify that you are running the BluetoothRfcommChat server.",
                return;
            }
            var serviceNameLength = attributeReader.ReadByte();

            // The Service Name attribute requires UTF-8 encoding.
            attributeReader.UnicodeEncoding = UnicodeEncoding.Utf8;

            StopSearch.Execute(null);

            lock (this)
            {
                m_chatSocket = new StreamSocket();
            }
            try
            {
                await m_chatSocket.ConnectAsync(m_chatService.ConnectionHostName, m_chatService.ConnectionServiceName);

                ClientViewType = ClientViewType.ChatView;
                m_chatWriter = new DataWriter(m_chatSocket.OutputStream);

                DataReader chatReader = new DataReader(m_chatSocket.InputStream);
                ReceiveStringLoop(chatReader);
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x80070490) // ERROR_ELEMENT_NOT_FOUND
            {
                //"Please verify that you are running the BluetoothRfcommChat server."
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x80072740) // WSAEADDRINUSE
            {
                //"Please verify that there is no other RFCOMM connection to the same device."
            }
        }
        public async void SendMessage(string message)
        {
            // There's no need to send a zero length message
            if (!string.IsNullOrEmpty(message))
            {
                // Make sure that the connection is still up and there is a message to send
                if (m_chatSocket != null)
                {
                    m_chatWriter.WriteUInt32((uint)message.Length);
                    m_chatWriter.WriteString(message);

                    MessageBoxList.Add("Sent: " + message);

                    await m_chatWriter.StoreAsync();
                }
                else
                {
                    //"No clients connected, please wait for a client to connect before attempting to send a message"
                }
            }
        }

        private async void ReceiveStringLoop(DataReader chatReader)
        {
            try
            {
                uint size = await chatReader.LoadAsync(sizeof(uint));
                if (size < sizeof(uint))
                {
                    //"Remote device terminated connection - make sure only one instance of server is running on remote device"
                    Disconnect();
                    return;
                }

                uint stringLength = chatReader.ReadUInt32();
                uint actualStringLength = await chatReader.LoadAsync(stringLength);
                if (actualStringLength != stringLength)
                {
                    // The underlying socket was closed before we were able to read the whole data
                    return;
                }

                MessageBoxList.Add("Received: " + chatReader.ReadString(stringLength));

                ReceiveStringLoop(chatReader);
            }
            catch (Exception ex)
            {
                lock (this)
                {
                    if (m_chatSocket == null)
                    {
                        // Do not print anything here -  the user closed the socket.
                        //if ((uint)ex.HResult == 0x80072745)
                        //    rootPage.NotifyUser("Disconnect triggered by remote device", NotifyType.StatusMessage);
                        //else if ((uint)ex.HResult == 0x800703E3)
                        //    rootPage.NotifyUser("The I/O operation has been aborted because of either a thread exit or an application request.", NotifyType.StatusMessage);
                    }
                    else
                    {
                        //"Read stream failed with error: " + ex.Message
                        Disconnect();
                    }
                }
            }
        }

        private void Disconnect()
        {
            if (m_chatWriter != null)
            {
                m_chatWriter.DetachStream();
                m_chatWriter = null;
            }

            if (m_chatService != null)
            {
                m_chatService.Dispose();
                m_chatService = null;
            }
            lock (this)
            {
                if (m_chatSocket != null)
                {
                    m_chatSocket.Dispose();
                    m_chatSocket = null;
                }
            }

            ClientViewType = ClientViewType.BluetoothConnectView;
        }

        private void StartUnpairedDeviceWatcher()
        {
            // Request additional properties
            string[] requestedProperties = new string[] { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

            m_deviceWatcher = DeviceInformation.CreateWatcher("(System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\")",
                                                            requestedProperties,
                                                            DeviceInformationKind.AssociationEndpoint);

            // Hook up handlers for the watcher events before starting the watcher
            m_deviceWatcher.Added += new TypedEventHandler<DeviceWatcher, DeviceInformation>((watcher, deviceInfo) =>
            {
                Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                     () =>
                           {
                               AvailableDevices.Add(deviceInfo);
                           });
            });

            m_deviceWatcher.Updated += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>((watcher, deviceInfoUpdate) =>
            {
                Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () =>
                        {
                            foreach (var rfcommInfoDisp in AvailableDevices)
                            {
                                if (rfcommInfoDisp.Id == deviceInfoUpdate.Id)
                                {
                                    rfcommInfoDisp.Update(deviceInfoUpdate);
                                    break;
                                }
                            }
                        });
            });
            m_deviceWatcher.Removed += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>((watcher, deviceInfoUpdate) =>
            {
                // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.

                // Find the corresponding DeviceInformation in the collection and remove it
                Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () =>
                    {
                        foreach (var rfcommInfoDisp in AvailableDevices)
                        {
                            if (rfcommInfoDisp.Id == deviceInfoUpdate.Id)
                            {
                                AvailableDevices.Remove(rfcommInfoDisp);
                                break;
                            }
                        }
                    });
            });

            m_deviceWatcher.Start();
        }
    }
}