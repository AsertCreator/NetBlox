using NetBlox;
using Timer = System.Windows.Forms.Timer;

namespace NetBloxDebug
{
	public partial class GameDebugForm : Form
	{
		public GameManager Attached;

		public GameDebugForm()
		{
			InitializeComponent();
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
		}

		private void AppendLog(object? sender, string message) => Invoke(() => gameLog.Text += message + "\r\n");

		~GameDebugForm()
		{
			LogManager.OnLog -= AppendLog;
		}
		public GameDebugForm(GameManager at)
		{
			InitializeComponent();
			Attached = at;
			Text = "NetBlox Debugger - " + at.ManagerName;

			LogManager.OnLog += AppendLog;

			var timer = new Timer();
			timer.Interval = 1000 / 2;
			timer.Tick += (_, _) =>
			{
				gameNameLabel.Text = "GameManager's name: " + at.ManagerName;
				gameUptimeLabel.Text = "GameManager's uptime: " + (DateTime.Now - at.TimeOfCreation);

				gameCharsLabel.Text = string.Concat(new string[]
				{
					at.NetworkManager.IsClientGame ? "Is client, " : "",
					at.NetworkManager.IsServerGame ? "Is server, " : "",
					(at.NetworkManager.NetworkClient!.RemoteConnection != null && at.NetworkManager.NetworkClient!.RemoteConnection.IsAlive) ? 
						"Connected to remote game, " : "",
					!at.NetworkManager.PolicyBroadcastedServer ? "Connected to internal game, " : "",
					at.RenderManager.RenderAtAll ? "Rendering, " : "",
					at.RenderManager.DebugInformation ? "Showing debug info, " : "",
					!at.PhysicsManager.DisablePhysics ? "Simulating physics, " : "",

					"Actor count = " + at.PhysicsManager.Actors.Count + ", ",
					"Clients count = " + at.NetworkManager.NetworkClient!.AllClients.Count + ", ",
					"Instances count = " + at.AllInstances.Count + ", ",
				});
			};
			timer.Start();
		}

		private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (Attached.IsRunning)
				pauseToolStripMenuItem.Text = "Resume heartbeat";
			else
				pauseToolStripMenuItem.Text = "Pause heartbeat";

			Attached.IsRunning = !Attached.IsRunning;
		}
		private void pausePhysicsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (Attached.PhysicsManager.DisablePhysics)
				pausePhysicsToolStripMenuItem.Text = "Pause physics";
			else
				pausePhysicsToolStripMenuItem.Text = "Resume physics";

			Attached.PhysicsManager.DisablePhysics = !Attached.PhysicsManager.DisablePhysics;
		}
		private void pauseRenderingToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (Attached.RenderManager.RenderAtAll)
				pauseRenderingToolStripMenuItem.Text = "Resume rendering";
			else
				pauseRenderingToolStripMenuItem.Text = "Pause rendering";

			Attached.RenderManager.RenderAtAll = !Attached.RenderManager.RenderAtAll;
		}
		private void shutdownThisInstanceToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("Are you sure you want to shutdown this GameManager? You know, something bad might happen?", "Hmmm", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
				Attached.Shutdown();
		}
		private void shutdownAllInstancesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("Are you sure you want to shutdown this NetBlox instance? Don't wanna play anymore?", "You aren't kidding?", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
				AppManager.Shutdown();
		}
		private void detachToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("Are you sure you want to detach from this GameManager? Current state of Game will be preserved as is.", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
				Close();
		}
		private void luaExecutorRun_Click(object sender, EventArgs e)
		{
			try
			{
				NetBlox.TaskScheduler.ScheduleScript(Attached, luaExecutorBox.Text, (int)luaExecutorLevelSelector.Value, null);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to run code, msg: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		private void clearLog_Click(object sender, EventArgs e)
		{
			gameLog.Clear();
		}
	}
}
