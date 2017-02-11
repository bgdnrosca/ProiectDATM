using BluetoothChat.Helpers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace BluetoothChat.UI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BluetoothClientView : NavigablePage
    {
        public BluetoothClientViewModel Vm => DataContext as BluetoothClientViewModel;

        public BluetoothClientView()
        {
            this.InitializeComponent();
        }

        public void KeyboardKey_Pressed(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                Vm.SendMessage(MessageTextBox.Text);
                MessageTextBox.Text = "";
            }
        }

        private void SendButton_OnClick(object sender, RoutedEventArgs e)
        {
            Vm.SendMessage(MessageTextBox.Text);
            MessageTextBox.Text = "";
        }
    }
}