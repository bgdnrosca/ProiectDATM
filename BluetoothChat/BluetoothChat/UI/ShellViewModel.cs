using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using BluetoothChat.Helpers;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Views;

namespace BluetoothChat.UI
{
    public enum PageType
    {
        ClientSide,
        ServerSide
    }
    public class ShellViewModel : ViewModelBase
    {
        private INavigationService m_navigationService;

        public object CurrentPage { get; set; }

        public string Header { get; set; }

        public RelayCommand<PageType> OpenPageCommand { get; }
        public ShellViewModel()
        {
            m_navigationService = NavigationHelper.GetNavigationService();
            OpenPageCommand = new RelayCommand<PageType>(OpenPageAsync);
            OpenPageAsync(PageType.ClientSide);
        }

        private async void OpenPageAsync(PageType page)
        {
            switch (page)
            {
                case PageType.ClientSide:
                    CurrentPage = new BluetoothClientView();
                    Header = "Client";
                    break;
                case PageType.ServerSide:
                    CurrentPage = new BluetoothServerView();
                    Header = "Server";
                    break;
            }
            RaisePropertyChanged(nameof(CurrentPage));
            RaisePropertyChanged(nameof(Header));
        }
    }
}
