using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BluetoothChat.CustomControls
{
    public sealed partial class LoadingSpinner : UserControl
    {
        public LoadingSpinner()
        {
            this.InitializeComponent();
            LayoutRoot.DataContext = this;
            BackgroundBrush = new SolidColorBrush(Colors.DarkSlateGray);
            BackgroundOpacity = .7;
        }

        #region STATIC FIELDS

        // -------------------------------------------------------------------------------------------------------

        public static readonly DependencyProperty IsExecutingProperty = DependencyProperty.Register(
            nameof(IsExecuting), typeof(bool), typeof(LoadingSpinner),
            new PropertyMetadata(default(bool), PropertyChangedCallback));

        public static readonly DependencyProperty BackgroundBrushProperty = DependencyProperty.Register(
            nameof(BackgroundBrush), typeof(Brush), typeof(LoadingSpinner), new PropertyMetadata(default(string)));

        public static readonly DependencyProperty BackgroundOpacityProperty = DependencyProperty.Register(
            nameof(BackgroundOpacity), typeof(double), typeof(LoadingSpinner), new PropertyMetadata(default(double)));

        #endregion STATIC FIELDS

        public bool IsExecuting
        {
            get { return (bool)GetValue(IsExecutingProperty); }
            set { SetValue(IsExecutingProperty, value); }
        }

        public Brush BackgroundBrush
        {
            get { return (Brush)GetValue(BackgroundBrushProperty); }
            set { SetValue(BackgroundBrushProperty, value); }
        }

        public double BackgroundOpacity
        {
            get { return (double)GetValue(BackgroundOpacityProperty); }
            set { SetValue(BackgroundOpacityProperty, value); }
        }

        private void OpacityAnimation_OnCompleted(object sender, object e)
        {
            if (RelativePanel.Opacity == 0)
            {
                RelativePanel.Visibility = Visibility.Collapsed;
            }
        }

        private static void PropertyChangedCallback(DependencyObject dobj, DependencyPropertyChangedEventArgs args)
        {
            LoadingSpinner spinner = (LoadingSpinner)dobj;
            spinner.RelativePanel.Visibility = (bool)args.NewValue ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}