using Microsoft.Web.WebView2.Wpf;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NetBlox.Studio
{
	/// <summary>
	/// Interaction logic for BrowserTabView.xaml
	/// </summary>
	public partial class BrowserTabView : System.Windows.Controls.UserControl
	{
		public BrowserTabView()
		{
			InitializeComponent();
		}
		public BrowserTabView(string url)
		{
			InitializeComponent();
			var wb = new WebView2();
			wb.Source = new Uri(url);
			wb.CoreWebView2InitializationCompleted += (x, y) =>
			{
				wb.CoreWebView2.Settings.AreDevToolsEnabled = false;
				wb.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
				wb.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
			};
			wb.SourceChanged += (x, y) =>
			{
				address.Text = wb.Source.ToString();
				back.IsEnabled = wb.CanGoBack;
				forward.IsEnabled = wb.CanGoForward;
			};
			back.Click += (x, y) =>
			{
				if (wb.CanGoBack)
					wb.GoBack();
			};
			forward.Click += (x, y) =>
			{
				if (wb.CanGoForward)
					wb.GoForward();
			};
			Grid.SetRow(wb, 2);
			address.IsReadOnly = true;
			address.Text = url;

			back.IsEnabled = false;
			forward.IsEnabled = false;

			System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
			dispatcherTimer.Tick += (x, y) =>
			{
				back.IsEnabled = wb.CanGoBack;
				forward.IsEnabled = wb.CanGoForward;
			};
			dispatcherTimer.Interval = TimeSpan.FromSeconds(0.33);
			dispatcherTimer.Start();


			root.Children.Add(wb);
		}
	}
}
