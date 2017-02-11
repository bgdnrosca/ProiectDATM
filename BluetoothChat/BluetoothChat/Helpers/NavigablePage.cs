using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace BluetoothChat.Helpers
{
    public class NavigablePage : Page
    {
        public static readonly DependencyProperty HeaderProperty =
                       DependencyProperty.Register(nameof(Header), typeof(object), typeof(NavigablePage), new PropertyMetadata(null));

        public object Header
        {
            get { return GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        protected virtual DisplayOrientations PreferredAppOrientation => DisplayOrientations.Portrait | DisplayOrientations.PortraitFlipped;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            this.DataContext = e.Parameter;
            //Only change app orientation on transition to a page with a different one
            if (PreferredAppOrientation != DisplayInformation.AutoRotationPreferences)
            {
                DisplayInformation.AutoRotationPreferences = PreferredAppOrientation;
            }
        }
    }
}