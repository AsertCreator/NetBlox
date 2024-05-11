using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WindowsNBServerManager
{
    /// <summary>
    /// Interaction logic for CreateServerDialog.xaml
    /// </summary>
    public partial class CreateServerDialog : Window
    {
        public CreateServerDialog()
        {
            InitializeComponent();
        }

        private void StartAction(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            App.SpawnServer(piifp.Text, (string.IsNullOrWhiteSpace(placo.Text) ? null : placo.Text), (string.IsNullOrWhiteSpace(univo.Text) ? null : univo.Text), int.Parse(maxpl.Text));
            Close();
            (Application.Current.MainWindow as MainWindow)!.ReloadServerList();
        }
    }
}
