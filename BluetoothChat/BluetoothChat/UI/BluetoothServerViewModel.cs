using BluetoothChat.Helpers;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;

namespace BluetoothChat.UI
{
    public enum ServerViewType
    {
        SetupServer,
        ChatView
    }

    public enum ServerStatus
    {
        Stopped,
        Started,
        FailToCreate
    }

    public class BluetoothServerViewModel : ViewModelBase
    {
        private RfcommServiceProvider m_rfcommServiceProvider;
        private StreamSocket m_chatSocker;
        private DataWriter m_dataWriter;
        private StreamSocketListener m_socketListener;

        private ServerStatus m_serverStatus;
        private ServerViewType m_serverView;
        private BluetoothDevice m_remoteDevice;

        public ObservableCollection<string> MessageBoxList { get; }

        public ServerStatus ServerStatus
        {
            get { return m_serverStatus; }
            set { Set(nameof(ServerStatus), ref m_serverStatus, value); }
        }

        public BluetoothDevice RemoteDevice
        {
            get { return m_remoteDevice; }
            set { Set(nameof(RemoteDevice), ref m_remoteDevice, value); }
        }

        public ServerViewType ServerViewType
        {
            get { return m_serverView; }
            set { Set(nameof(ServerViewType), ref m_serverView, value); }
        }

        public ICommand StartServer { get; }

        public ICommand StopServer { get; }

        public ICommand DisconnectCommand { get; }

        public BluetoothServerViewModel()
        {
            ServerStatus = ServerStatus.Stopped;
            MessageBoxList = new ObservableCollection<string>();
            StartServer = new RelayCommand(StartServerAsync);
            StopServer = new RelayCommand(StopServerAsync);
            ServerViewType = ServerViewType.SetupServer;
            DisconnectCommand = new RelayCommand(Disconnect);
        }

        private void StopServerAsync()
        {
            if (m_rfcommServiceProvider != null)
            {
                m_rfcommServiceProvider.StopAdvertising();
                m_rfcommServiceProvider = null;
            }

            if (m_socketListener != null)
            {
                m_socketListener.Dispose();
                m_socketListener = null;
            }

            if (m_dataWriter != null)
            {
                m_dataWriter.DetachStream();
                m_dataWriter = null;
            }

            if (m_dataWriter != null)
            {
                m_dataWriter.Dispose();
                m_dataWriter = null;
            }
            ServerStatus = ServerStatus.Stopped;
        }

        private async void StartServerAsync()
        {
            try
            {
                m_rfcommServiceProvider =
                    await RfcommServiceProvider.CreateAsync(RfcommServiceId.FromUuid(Constants.BluetoothDatmGUID));
            }
            catch (Exception ex)
            {
                //TODO Log this error and show a message on UI
                ServerStatus = ServerStatus.FailToCreate;
                return;
            }

            m_socketListener = new StreamSocketListener();
            m_socketListener.ConnectionReceived += OnConnectionReceived;

            await m_socketListener.BindServiceNameAsync(m_rfcommServiceProvider.ServiceId.AsString(),
                SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);

            // Set the SDP attributes and start Bluetooth advertising
            InitializeServiceSdpAttributes(m_rfcommServiceProvider);

            try
            {
                m_rfcommServiceProvider.StartAdvertising(m_socketListener, true);
            }
            catch (Exception e)
            {
                ServerStatus = ServerStatus.FailToCreate;
                return;
            }
            ServerStatus = ServerStatus.Started;
        }

        private void InitializeServiceSdpAttributes(RfcommServiceProvider rfcommProvider)
        {
            var sdpWriter = new DataWriter();

            // Write the Service Name Attribute.
            sdpWriter.WriteByte(Constants.SdpServiceNameAttributeType);

            // The length of the UTF-8 encoded Service Name SDP Attribute.
            sdpWriter.WriteByte((byte)Constants.SdpServiceName.Length);

            // The UTF-8 encoded Service Name value.
            sdpWriter.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
            sdpWriter.WriteString(Constants.SdpServiceName);

            // Set the SDP Attribute on the RFCOMM Service Provider.
            rfcommProvider.SdpRawAttributes.Add(Constants.SdpServiceNameAttributeId, sdpWriter.DetachBuffer());
        }

        public async void SendMessage(string message)
        {
            // There's no need to send a zero length message
            if (!string.IsNullOrEmpty(message))
            {
                // Make sure that the connection is still up and there is a message to send
                if (m_chatSocker != null)
                {
                    m_dataWriter.WriteUInt32((uint)message.Length);
                    m_dataWriter.WriteString(message);

                    MessageBoxList.Add("Sent: " + message);

                    await m_dataWriter.StoreAsync();
                }
                else
                {
                    //"No clients connected, please wait for a client to connect before attempting to send a message"
                }
            }
        }

        private async void OnConnectionReceived(StreamSocketListener sender,
            StreamSocketListenerConnectionReceivedEventArgs args)
        {
            // Don't need the listener anymore
            m_socketListener.Dispose();
            m_socketListener = null;

            try
            {
                m_chatSocker = args.Socket;
            }
            catch (Exception e)
            {
                Disconnect();
                return;
            }
            var remoteDevice = await BluetoothDevice.FromHostNameAsync(m_chatSocker.Information.RemoteHostName);

            m_dataWriter = new DataWriter(m_chatSocker.OutputStream);
            var reader = new DataReader(m_chatSocker.InputStream);
            bool remoteDisconnection = false;

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                     async () =>
                     {
                         MessageBoxList.Clear();
                         RemoteDevice = remoteDevice;
                         ServerViewType = ServerViewType.ChatView;
                     });

            // Infinite read buffer loop
            while (true)
            {
                try
                {
                    // Based on the protocol we've defined, the first uint is the size of the message
                    uint readLength = await reader.LoadAsync(sizeof(uint));

                    // Check if the size of the data is expected (otherwise the remote has already terminated the connection)
                    if (readLength < sizeof(uint))
                    {
                        remoteDisconnection = true;
                        break;
                    }
                    uint currentLength = reader.ReadUInt32();

                    // Load the rest of the message since you already know the length of the data expected.
                    readLength = await reader.LoadAsync(currentLength);

                    // Check if the size of the data is expected (otherwise the remote has already terminated the connection)
                    if (readLength < currentLength)
                    {
                        remoteDisconnection = true;
                        break;
                    }
                    string message = reader.ReadString(currentLength);
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                     async () =>
                     {
                         MessageBoxList.Add("Received: " + message);
                     });
                }
                // Catch exception HRESULT_FROM_WIN32(ERROR_OPERATION_ABORTED).
                catch (Exception ex) when ((uint)ex.HResult == 0x800703E3)
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        async () =>
                        {
                            MessageBoxList.Add("Client Disconnected Successfully");
                        });
                    break;
                }
            }

            reader.DetachStream();
            if (remoteDisconnection)
            {
                Disconnect();
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                 async () =>
                 {
                     MessageBoxList.Add("Client disconnected");
                 });
            }
        }

        private async void Disconnect()
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                async () =>
                {
                    ServerViewType = ServerViewType.SetupServer;
                });
            if (m_rfcommServiceProvider != null)
            {
                m_rfcommServiceProvider.StopAdvertising();
                m_rfcommServiceProvider = null;
            }

            if (m_socketListener != null)
            {
                m_socketListener.Dispose();
                m_socketListener = null;
            }

            if (m_dataWriter != null)
            {
                m_dataWriter.DetachStream();
                m_dataWriter = null;
            }

            if (m_chatSocker != null)
            {
                m_chatSocker.Dispose();
                m_chatSocker = null;
            }
        }
    }
}