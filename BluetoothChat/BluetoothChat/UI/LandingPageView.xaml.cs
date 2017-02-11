using Windows.UI.Xaml.Controls;
using BluetoothChat.Helpers;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace BluetoothChat.UI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LandingPageView : NavigablePage
    {
        public LandingPageViewModel Vm => DataContext as LandingPageViewModel;

        public LandingPageView()
        {
            this.InitializeComponent();
        }
    }
}