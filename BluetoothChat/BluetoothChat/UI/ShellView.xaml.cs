﻿using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace BluetoothChat.UI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ShellView : Page
    {
        public ShellViewModel Vm => DataContext as ShellViewModel;

        public ShellView(Frame frame)
        {
            this.InitializeComponent();
            MainContent.Content = frame;
        }
    }
}