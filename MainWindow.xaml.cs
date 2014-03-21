// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="FreeToDev"> (c) Mike Fourie. All other rights reserved.</copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace LineCounter
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Windows;
    using System.Windows.Forms;

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow
    {
        private readonly OpenFileDialog ofd = new OpenFileDialog();

        public MainWindow()
        {
            this.InitializeComponent();
        }

        internal void PopulatePath()
        {
            try
            {
                DirectoryInfo d = new DirectoryInfo(System.Windows.Forms.Clipboard.GetText());
                if (d.Exists)
                {
                    this.txtPath.Text = d.FullName;
                }
            }
            catch
            {
                // just do nothing.
            }
        }

        private void BtnScan_OnClick(object sender, RoutedEventArgs e)
        {
            DateTime start = DateTime.Now;
            if (Directory.Exists(this.txtPath.Text))
            {
                this.ctrlLineCounter.Scan();
                DateTime end = DateTime.Now;
                TimeSpan t = end - start;
                this.txtStatus.Text = "Scan Time: " + t.Seconds + " seconds and " + t.Milliseconds + " milliseconds";
            }
            else
            {
                this.txtStatus.Text = "Path not found...";
            }
        }

        private void BtnContributor_OnClick(object sender, RoutedEventArgs e)
        {
            this.ctrlContributors.Visibility = Visibility.Visible;
            this.ctrlLineCounter.Visibility = Visibility.Hidden;
        }

        private void BtnOpen_OnClick(object sender, RoutedEventArgs e)
        {
            this.ofd.Title = "Open File Category JSON Configuration File";
            this.ofd.Multiselect = false;
            this.ofd.RestoreDirectory = true;
            this.ofd.Filter = "JSON Files|*.json|All files|*.*";
            this.ofd.FilterIndex = 0;
            System.Windows.Forms.DialogResult result = this.ofd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                this.ctrlLineCounter.OpenJson(this.ofd.FileName);
            }
        }

        private void BtnCopyResults_OnClick(object sender, RoutedEventArgs e)
        {
            this.ctrlLineCounter.CopyResults();
        }

        private void BtnBrowse_OnClick(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog { Description = "Select a folder", SelectedPath = Directory.Exists(this.txtPath.Text) ? this.txtPath.Text : Environment.SpecialFolder.MyComputer.ToString() };
            System.Windows.Forms.DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                this.txtPath.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void RibbonWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.ctrlLineCounter.LoadCategories("DefaultFileCategories.json");
            this.PopulatePath();
        }
    }
}
