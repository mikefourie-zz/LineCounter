// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Contributors.xaml.cs" company="FreeToDev"> (c) Mike Fourie. All other rights reserved.</copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace LineCounter.UserControls
{
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Navigation;

    /// <summary>
    /// Interaction logic for Contributors
    /// </summary>
    public partial class Contributors
    {
        private MainWindow parentWindow;

        public Contributors()
        {
            this.InitializeComponent();
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.parentWindow.ctrlContributors.Visibility = Visibility.Hidden;
            this.parentWindow.ctrlLineCounter.Visibility = Visibility.Visible;
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            this.parentWindow = (MainWindow)Window.GetWindow(this);
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
