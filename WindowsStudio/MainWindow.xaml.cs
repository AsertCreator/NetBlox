using Microsoft.Web.WebView2.Wpf;
using NetBlox;
using NetBlox.Instances;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using Raylib_cs;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Forms.Integration;

namespace WindowsStudio;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : System.Windows.Window
{
	public static GameManager? Baseplate;
	public static MainWindow? Instance;

	public MainWindow()
	{
		Instance = this;
		InitializeComponent();
		Application.EnableVisualStyles();
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
			OpenEditorTab();
		};
	}
	public unsafe void OpenEditorTab()
	{
		var ti = OpenTab("Baseplate (edit)");
		var wfh = new WindowsFormsHost();
		ti.Content = wfh;
		var pan = new System.Windows.Forms.Panel();
		var label = new System.Windows.Forms.Label();
		label.Parent = pan;
		label.Location = new Point(50, 50);
		label.AutoSize = true;
		label.Text = "NetBlox is loading, please wait...";
		label.ForeColor = System.Drawing.Color.White;
		pan.BackColor = System.Drawing.Color.Black;
		wfh.Child = pan;
		var panh = pan.Handle;

		Task.Run(() =>
		{
			Baseplate = AppManager.CreateGame(new GameConfiguration()
			{
				AsServer = true,
				AsStudio = true,
				CustomFlags = ConfigFlags.UndecoratedWindow | ConfigFlags.MaximizedWindow | ConfigFlags.HiddenWindow,
				ProhibitScripts = true,
				GameName = "NetBlox Studio - Baseplate"
			}, [], (x, y) => { });
			Baseplate.LoadDefault();
			AppManager.PlatformOpenBrowser = x =>
			{
				Dispatcher.Invoke(() =>
				{
					OpenBrowserTab("Browser, opened by user code", x);
				});
			};
			AppManager.SetRenderTarget(Baseplate);
			while (true)
			{
				var h = (nint)Raylib.GetWindowHandle();
				if (h < 0) continue;
				SetParent(h, panh);
				// SetWindowLongPtr(h, -16, 1342177280); it doesnt fucking work because keyboard doesnt passthrough help my ass
				ShowWindow(h, 3);
				MoveWindow(h, 0, 0, pan.Width, pan.Height, true);
				// EnableWindow(h, true);
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
		var wb = new BrowserTabView(url);
		ti.Content = wb;
	}
	[DllImport("user32.dll", SetLastError = true)]
	static extern IntPtr SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);
	[DllImport("user32.dll", SetLastError = true)]
	static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent); 
	[DllImport("user32.dll")]
	static extern bool EnableWindow(IntPtr hWnd, bool bEnable);
	[DllImport("user32.dll", SetLastError = true)]
	static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
	[DllImport("user32.dll")]
	static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

	private void PartButtonClick(object sender, System.Windows.RoutedEventArgs e)
	{
		if (Baseplate != null) 
		{
			Workspace works = Baseplate.CurrentRoot.GetService<Workspace>();
			Part part = new(Baseplate);

			part.Size = new System.Numerics.Vector3(4, 1, 2);
			part.Position = Baseplate.RenderManager.MainCamera.Position;
			part.Color = Color.Gray;
			part.Parent = works;
		}
	}
	private void StartPlaybackClick(object sender, System.Windows.RoutedEventArgs e)
	{
		if (Baseplate != null)
		{
			// first we clone the datamodel
			// then we reassign it to new server gamemanager
			// then we start a server
			// then we start a client (basically a duohost)

			play.IsEnabled = false;
			stop.IsEnabled = true;

			DataModel dm = (DataModel)Baseplate.CurrentRoot.ForceClone();
			GameManager gms = AppManager.CreateGame(new GameConfiguration()
			{
				AsServer = true,
				AsStudio = true,
				SkipWindowCreation = true,
				DoNotRenderAtAll = true,
				GameName = "NetBlox Server (studio)"
			}, [], (x, gm) =>
			{
				gm.CurrentRoot.ClearAllChildren();

				gm.CurrentIdentity.PlaceName = "";
				gm.CurrentIdentity.UniverseName = "";
				gm.CurrentIdentity.Author = "";

				for (int i = 0; i < dm.GetChildren().Length; i++)
				{
					var d = dm.GetChildren()[i];
					d.ChangeOwnership(gm);
					d.Parent = gm.CurrentRoot;
				}

				Task.Run(gm.NetworkManager.StartServer);

				GameManager gmc = null!;
				PlatformService.QueuedTeleport = (xo) =>
				{
					gmc.NetworkManager.ClientReplicator = Task.Run(async delegate ()
					{
						try
						{
							gmc.NetworkManager.ConnectToServer(IPAddress.Loopback);
							return new object();
						}
						catch (Exception ex)
						{
							gmc.RenderManager.Status = "Could not connect to the server: " + ex.Message;
							return new();
						}
					}).AsCancellable(gmc.NetworkManager.ClientReplicatorCanceller.Token);
				};
				gmc = AppManager.CreateGame(new GameConfiguration()
				{
					AsClient = true,
					AsStudio = true,
					SkipWindowCreation = true,
					GameName = "NetBlox Client (studio)"
				}, ["--guest"], (x, y) => { });
				AppManager.SetRenderTarget(gmc);
			});
		}
	}
	private void commandBar_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (Baseplate != null && e.Key == System.Windows.Input.Key.Return)
		{
			LuaRuntime.Execute(commandBar.Text, 4, Baseplate, null);
		}
    }
}
