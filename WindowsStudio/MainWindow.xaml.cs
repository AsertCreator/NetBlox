using Microsoft.Web.WebView2.Wpf;
using NetBlox;
using Raylib_CsLo;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Forms.Integration;

namespace WindowsStudio;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : System.Windows.Window
{
	public static MainWindow? Instance;

	public MainWindow()
	{
		Instance = this;
		InitializeComponent();
		System.Windows.Forms.Application.EnableVisualStyles();
		OpenStartupTab();
	}
	public TabItem OpenTab(string title)
	{
		var ti = new TabItem();
		var cm = new ContextMenu();
		var em = new MenuItem() { Header = "Close tab" };
		em.Click += (x, y) =>
		{
			tabs.Items.Remove(ti);
		};
		cm.Items.Add(em);
		ti.Header = new ContentControl()
		{
			Content = title,
			ContextMenu = cm
		};
		tabs.Items.Add(ti);
		tabs.SelectedItem = ti;
		return ti;
	}
	public void OpenStartupTab()
	{
		var ti = OpenTab("Start Page");
		var wb = new StartupTabView();
		ti.Content = wb;

		wb.githubLink.Click += (x, y) =>
		{
			OpenBrowserTab("GitHub", "https://github.com/AsertCreator/NetBlox");
		};
		wb.openBaseplate.Click += (x, y) =>
		{
			OpenGameTab();
		};
	}
	public unsafe void OpenGameTab()
	{
		var ti = OpenTab("Game");
		var wfh = new WindowsFormsHost();
		ti.Content = wfh;
		var pan = new System.Windows.Forms.Panel();
		pan.BackColor = System.Drawing.Color.Black;
		wfh.Child = pan;
		var panh = pan.Handle;

		Task.Run(() =>
		{
			var tgm = AppManager.CreateGame(new GameConfiguration()
			{
				AsServer = true,
				AsStudio = true,
				CustomFlags = ConfigFlags.FLAG_WINDOW_UNDECORATED | ConfigFlags.FLAG_WINDOW_MAXIMIZED | ConfigFlags.FLAG_WINDOW_HIDDEN,
				ProhibitScripts = true,
				GameName = "NetBlox Studio - Title"
			}, [], (x, y) => { });
			tgm.LoadDefault();
			AppManager.PlatformOpenBrowser = x =>
			{
				Dispatcher.Invoke(() =>
				{
					OpenBrowserTab("Browser, opened by user code", x);
				});
			};
			AppManager.SetRenderTarget(tgm);
			while (true)
			{
				var h = (nint)Raylib.GetWindowHandle();
				if (h < 0) continue;
				SetParent(h, panh);
				ShowWindow(h, 3);
				MoveWindow((nint)Raylib.GetWindowHandle(), 0, 0, pan.Width, pan.Height, true);
				break;
			}

			pan.Resize += (x, y) =>
			{
				MoveWindow((nint)Raylib.GetWindowHandle(), 0, 0, pan.Width, pan.Height, true);
			};

			AppManager.Start();
		});
	}
	public void OpenBrowserTab(string header, string url)
	{
		var ti = OpenTab(header);
		var wb = new WebView2();
		wb.Source = new Uri(url);
		wb.CoreWebView2InitializationCompleted += (x, y) =>
		{
			wb.CoreWebView2.Settings.AreDevToolsEnabled = false;
			wb.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
			wb.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
		};
		ti.Content = wb;
	}
	[DllImport("user32.dll", SetLastError = true)]
	static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
	[DllImport("user32.dll", SetLastError = true)]
	static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
	[DllImport("user32.dll")]
	static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
