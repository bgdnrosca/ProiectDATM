using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Views;
using Windows.UI.Popups;

namespace BluetoothChat.Helpers
{
    public class NavigationHelper
    {
        public static INavigationService NavigationService { get; private set; }
        public NavigationHelper(INavigationService navService)
        {
            NavigationService = navService;
        }

        public static INavigationService GetNavigationService()
        {
            return NavigationService;
        }

        public static async Task ShowPopup(string message)
        {
            var popup = new MessageDialog(message);
            await popup.ShowAsync();
        }
    }
}