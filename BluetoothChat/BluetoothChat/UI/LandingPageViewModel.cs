using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using BluetoothChat.Helpers;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace BluetoothChat.UI
{
    public class LandingPageViewModel : ViewModelBase
    {
        private INavigationService m_navigationService;
        public RelayCommand<bool> NavigateToServerOrClientPage { get; }

        public LandingPageViewModel(INavigationService navService)
        {
            m_navigationService = navService;
            NavigateToServerOrClientPage = new RelayCommand<bool>(NavigateAway);
        }

        private void NavigateAway(bool goToServerMode)
        {
            if (goToServerMode)
            {
                m_navigationService.Navigate(new BluetoothServerViewModel());
            }
            else
            {
                m_navigationService.Navigate(new BluetoothClientViewModel());
            }
        }
    }
}
