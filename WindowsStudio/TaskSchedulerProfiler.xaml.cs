using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace NetBlox.Studio
{
	public class TaskInfo
	{
		public Job Job;
		public double JobTime;
		public double JobPercent;
	}
	/// <summary>
	/// Interaction logic for TaskSchedulerProfiler.xaml
	/// </summary>
	public partial class TaskSchedulerProfiler : System.Windows.Controls.UserControl
	{
		public List<TaskInfo> CurrentTasks = [];
		public TaskSchedulerProfiler()
		{
			InitializeComponent();
		}
		public void Recalculate()
		{
			List<string> types = [];
			List<double> percn = [];
			List<double> times = [];
			double overall = 0; // i dont believe my own code
			TaskScheduler.RunningJobs.ForEach(x =>
			{
				types.Add(x.Type.ToString());
				times.Add(x.LastCycleTime);
				overall += x.LastCycleTime;
			});

			CurrentTasks.Clear();

			for (int i = 0; i < times.Count; i++)
				CurrentTasks.Add(new TaskInfo()
				{
					Job = TaskScheduler.RunningJobs[i],
					JobTime = times[i],
					JobPercent = (int)(times[i] / overall * 100)
				});
		}
		public void Revisualize()
		{
			tasks.Items.Clear();
			for (int i = 0; i < CurrentTasks.Count; i++)
			{
				var j = i;
				var ctxmenu = new ContextMenu();

				MenuItem MakeMenuItem(string text, Action act)
				{
					MenuItem mi = new();
					mi.Header = text;
					mi.Click += (x, y) => act();
					return mi;
				}

				ctxmenu.Items.Add(MakeMenuItem("Destroy", () => TaskScheduler.Terminate(CurrentTasks[j].Job)));
				ctxmenu.Items.Add(MakeMenuItem("Increment priority", () => CurrentTasks[j].Job.Priority++));
				ctxmenu.Items.Add(MakeMenuItem("Decrement priority", () => CurrentTasks[j].Job.Priority--));

				tasks.Items.Add(new StackPanel()
				{
					Children =
					{
						new TextBlock() { Text = CurrentTasks[i].Job.Name + " - " + CurrentTasks[i].Job.Type, FontWeight = FontWeight.FromOpenTypeWeight(700) },
						new TextBlock() { Text = CurrentTasks[i].JobPercent.ToString() + "% of whole time, " + CurrentTasks[i].JobTime + " ms" },
					},
					ContextMenu = ctxmenu
				});
			}
		}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Recalculate();
			Revisualize();
		}
	}
}
