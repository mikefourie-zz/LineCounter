// --------------------------------------------------------------------------------------------------------------------
// <copyright file="App.xaml.cs" company="FreeToDev"> (c) Mike Fourie. All other rights reserved.</copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace LineCounter
{
    using System.Globalization;
    using System.Windows;

    /// <summary>
    /// Interaction logic for App
    /// </summary>
    public partial class App
    {
        public App()
        {
            this.Dispatcher.UnhandledException += this.OnDispatcherUnhandledException;
        }

        internal void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string exception = e.Exception.ToString();
            if (e.Exception.InnerException != null)
            {
                exception += ". Inner Exception = " + e.Exception.InnerException;
            }

            string errorMessage = string.Format(CultureInfo.CurrentCulture, "{0}. Please report this error", exception);
            MessageBox.Show(errorMessage, "Sorry, this was not meant to happen.", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
