using BluetoothChat.Helpers;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace BluetoothChat.UI
{
    public class ShellViewModel : ViewModelBase
    {
        public string Header { get; set; }

        public ShellViewModel(INavigationService navService)
        {
            navService.Navigate(new LandingPageViewModel(navService));
        }
    }
}