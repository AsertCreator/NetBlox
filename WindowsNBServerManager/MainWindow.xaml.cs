using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace WindowsNBServerManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            bool valid = false;

            InitializeComponent();

            while (!valid)
            {
                OpenFolderDialog ofd = new();
                ofd.Title = "Select NetBlox server root folder";
                ofd.ShowDialog();
                string fn = ofd.FolderName;

                if (!File.Exists(Path.Combine(fn, App.ServerExecutable)))
                {
                    MessageBox.Show(this, $"That is not a NetBlox server root folder (root folder is that one which contains {App.ServerExecutable} file)", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }

                App.TargetExecutable = Path.Combine(fn, App.ServerExecutable);
                valid = true;
            }

            ReloadServerList();
        }
        public void ReloadServerList()
        {
            App.ReloadServerList();
            servers.ItemsSource = App.Servers;
            servers.Items.Refresh();
            status.Text = $"NetBlox v?.?.? - {App.Servers.Count} servers running";
        }
        private void ReloadServerListAction(object s, RoutedEventArgs e) => ReloadServerList();
        private void ShutdownServersAction(object sender, RoutedEventArgs e) 
        {
            if (MessageBox.Show(this, "You sure?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                App.ShutdownAllServers();
                Thread.Sleep(500);
                ReloadServerList();
            }
        }
        private void CreateNewServerAction(object sender, RoutedEventArgs e)
        {
            CreateServerDialog csd = new();
            csd.Owner = this;
            csd.Show();
            csd.Activate();
        }
    }
}