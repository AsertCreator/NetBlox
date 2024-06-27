using Microsoft.Web.WebView2.Wpf;
using NetBlox;
using NetBlox.Instances;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using Raylib_cs;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Windows.Controls;
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
			part.Color = Color.Gray;
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
		if (App.EditorGame != null && e.Key == System.Windows.Input.Key.Return)
		{
			LuaRuntime.Execute(commandBar.Text, 4, App.EditorGame, null);
		}
    }
}
