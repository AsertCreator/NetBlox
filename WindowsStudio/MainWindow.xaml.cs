using Microsoft.Web.WebView2.Wpf;
using NetBlox;
using NetBlox.Instances;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Forms.Integration;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace NetBlox.Studio;

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
		var ti = OpenTab("EditorGame (edit)");
		var wfh = new EditorTabView(this);
		ti.Content = wfh;
	}
	public void OpenBrowserTab(string header, string url)
	{
		var ti = OpenTab(header);
		var wb = new BrowserTabView(url);
		ti.Content = wb;
	}
	public void OpenScriptTab(BaseScript bs)
	{
		var ti = OpenTab(bs.GetFullName() + " (script)");
		var wb = new ScriptEditorView(bs);
		ti.Content = wb;
	}
	private void PartButtonClick(object sender, System.Windows.RoutedEventArgs e)
	{
		if (App.EditorGame != null) 
		{
			Workspace works = App.EditorGame.CurrentRoot.GetService<Workspace>();
			Part part = new(App.EditorGame);

			part.Size = new System.Numerics.Vector3(4, 1, 2);
			part.Position = App.EditorGame.RenderManager.MainCamera.Position;
			part.Color3 = Color.Gray;
			part.Parent = works;
		}
	}
	private void StartPlaybackClick(object sender, System.Windows.RoutedEventArgs e)
	{
		if (App.EditorGame != null)
		{
			// first we clone the datamodel
			// then we reassign it to new server gamemanager
			// then we start a server
			// then we start a client (basically a duohost)

			play.IsEnabled = false;
			stop.IsEnabled = true;

			DataModel dm = (DataModel)App.EditorGame.CurrentRoot.ForceClone();
			App.ServerGame = AppManager.CreateGame(new GameConfiguration()
			{
				AsServer = true,
				AsStudio = true,
				SkipWindowCreation = true,
				DoNotRenderAtAll = true,
				GameName = "NetBlox Server (studio)"
			}, ["-ss", "{}"], (gm) =>
			{
				gm.CurrentRoot.ClearAllChildren();

				gm.CurrentIdentity.PlaceName = "Personal Place";
				gm.CurrentIdentity.UniverseName = "NetBlox Studio";
				gm.CurrentIdentity.Author = "NetBlox";
				gm.CurrentIdentity.MaxPlayerCount = 5;

				var chd = dm.GetChildren();

				for (int i = 0; i < chd.Length; i++)
				{
					var d = chd[i];
					d.ChangeOwnership(gm);
					d.Parent = gm.CurrentRoot;
				}

				gm.NetworkManager.OnlyInternalConnections = true;
				Task.Run(gm.NetworkManager.StartServer);

				PlatformService.QueuedTeleport = (xo) =>
				{
					Debug.Assert(App.ClientGame != null);
					App.ClientGame.NetworkManager.ClientReplicator = Task.Run(async delegate ()
					{
						try
						{
							await Task.Delay(0);
							App.ClientGame.NetworkManager.ConnectToServer(IPAddress.Loopback);
							return new object();
						}
						catch (Exception ex)
						{
							App.ClientGame.RenderManager.Status = "Could not connect to the internal server: " + ex.Message;
							return new();
						}
					}).AsCancellable(App.ClientGame.NetworkManager.ClientReplicatorCanceller.Token);
				};

				App.ClientGame = AppManager.CreateGame(new GameConfiguration()
				{
					AsClient = true,
					AsStudio = true,
					SkipWindowCreation = true,
					GameName = "NetBlox Client (studio)"
				}, ["-cs", SerializationManager.SerializeJson<ClientStartupInfo>(new() {
					IsGuest = true
				})], (x) => { });

				AppManager.SetRenderTarget(App.ClientGame);
				App.ClientGame.ShutdownEvent += (x, y) =>
				{
					App.ServerGame?.Shutdown();
					App.ClientGame = null;
					App.ServerGame = null;
					GC.Collect(); // to be unnecessary mean
					AppManager.SetRenderTarget(App.EditorGame);
					Dispatcher.Invoke(() =>
					{
						play.IsEnabled = true;
						stop.IsEnabled = false;
					});
				};
			});
		}
	}
	private void commandBar_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (AppManager.CurrentRenderManager != null && e.Key == System.Windows.Input.Key.Return)
		{
			TaskScheduler.ScheduleScript(AppManager.CurrentRenderManager.GameManager, commandBar.Text, 4, null);
		}
	}
	private void ToggleDebugClick(object sender, System.Windows.RoutedEventArgs e)
	{
		if (AppManager.CurrentRenderManager != null)
		{
			AppManager.CurrentRenderManager.DebugInformation = (sender as RibbonToggleButton)!.IsChecked!.Value;
		}
	}
	private void StopButtonClick(object sender, System.Windows.RoutedEventArgs e)
	{
		if (App.ClientGame != null)
			App.ClientGame.Shutdown();
	}
	private void ShowAccounting(object sender, System.Windows.RoutedEventArgs e)
	{
		OpenTab("Task Scheduler Profiler").Content = new TaskSchedulerProfiler();
	}
}
