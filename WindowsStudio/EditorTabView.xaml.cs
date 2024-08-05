using NetBlox;
using NetBlox.Instances;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using Raylib_cs;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NetBlox.Studio
{
	/// <summary>
	/// Interaction logic for EditorTabView.xaml
	/// </summary>
	public partial class EditorTabView : System.Windows.Controls.UserControl
	{
		public static Dictionary<Instance, TreeViewItem> Items = [];
		public PropertyGrid propertyGridWF = new();
		public EditorTabView()
		{
			InitializeComponent();
		}
		public unsafe EditorTabView(MainWindow mw)
		{
			InitializeComponent();

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

			propertyGrid.Child = propertyGridWF;
			propertyGridWF.Visible = true;
			propertyGridWF.CommandsVisibleIfAvailable = false;
			propertyGridWF.Location = new Point(0, 0);
			propertyGridWF.HelpVisible = false;
			propertyGridWF.Size = new Size((int)propertyGrid.ActualWidth, (int)propertyGrid.ActualHeight);
			propertyGrid.SizeChanged += (x, y) => propertyGridWF.Size = new Size((int)propertyGrid.ActualWidth, (int)propertyGrid.ActualHeight);
			explorerTree.SelectedItemChanged += (x, y) =>
			{
				var inst = Items.FirstOrDefault(x => x.Value == y.NewValue).Key;
				DynamicTypeDescriptor dt = new DynamicTypeDescriptor(inst.GetType());
				dt.RemoveProperty("RequiredCapabilities");
				dt.RemoveProperty("DescendantRemoved");
				dt.RemoveProperty("DescendantAdded");
				dt.RemoveProperty("ChildRemoved");
				dt.RemoveProperty("ChildAdded");
				dt.RemoveProperty("UniqueID");
				dt.RemoveProperty("ParentID");
				dt.RemoveProperty("Source");
				for (int i = 0; i < dt.Properties.Count; i++)
				{
					var prop = dt.Properties[i];
					if (prop.PropertyType.IsAssignableTo(typeof(LuaSignal)))
						dt.RemoveProperty(prop.Name);
				}
				propertyGridWF.SelectedObject = dt.FromComponent(inst);
			};

			Task.Run(() =>
			{
				App.EditorGame = AppManager.CreateGame(new GameConfiguration()
				{
					AsServer = true,
					AsStudio = true,
					CustomFlags = ConfigFlags.UndecoratedWindow | ConfigFlags.MaximizedWindow | ConfigFlags.HiddenWindow,
					ProhibitScripts = true,
					GameName = "NetBlox Studio - EditorGame"
				}, ["-ss", "{}"], (x) => { }, x =>
				{
					TreeViewItem MakeItem(Instance inst)
					{
						var tvi = new TreeViewItem();
						var stack = new StackPanel();
						var block = new TextBlock();
						var image = new System.Windows.Controls.Image();
						var logo = new BitmapImage();
						block.Text = inst.Name;
						block.Margin = new System.Windows.Thickness(5, 0, 0, 0);
						logo.BeginInit();
						var classpng = AppManager.ResolveUrlAsync("rbxasset://studio/classes/" + inst.ClassName + ".png", false).WaitAndGetResult();
						if (File.Exists(classpng))
							logo.UriSource = new Uri(classpng);
						else
							logo.UriSource = new Uri(AppManager.ResolveUrlAsync("rbxasset://studio/classes/Instance.png", false).WaitAndGetResult());
						logo.EndInit();
						image.Source = logo;
						stack.Orientation = System.Windows.Controls.Orientation.Horizontal;
						stack.Children.Add(image);
						stack.Children.Add(block);
						tvi.Header = stack;
						MenuItem MakeItem(string text, bool enabled, Action act)
						{
							MenuItem mi = new();
							mi.Header = text;
							mi.Click += (x, y) => act();
							mi.IsEnabled = enabled;
							return mi;
						}
						MenuItem MakeInsertItem()
						{
							MenuItem mi = new();
							mi.Header = "Insert";
							Type[] typs = InstanceCreator.CreatableInstanceTypes;
							for (int i = 0; i < typs.Length; i++)
							{
								int j = i;
								MenuItem ins = new();
								ins.Header = typs[i].Name;
								ins.Click += (x, y) =>
								{
									var fabm = typs[j].GetMethod("Fabricate");
									if (fabm != null)
									{
										var ins = (Instance)fabm.Invoke(null, [App.EditorGame]);
										ins!.Parent = inst;
									}
									else
									{
										Instance ins = (Instance)Activator.CreateInstance(typs[j], App.EditorGame);
										ins!.Parent = inst;
									}
								};
								mi.Items.Add(ins);
							}
							return mi;
						}
						tvi.ContextMenu = new ContextMenu()
						{
							Items =
							{
								MakeItem("Destroy", true, () =>
								{
									inst.Destroy();
								}),
								MakeItem("Clear all children", true, () =>
								{
									inst.ClearAllChildren();
								}),
								MakeItem("Edit script", inst is BaseScript, () =>
								{
									MainWindow.Instance!.OpenScriptTab((inst as BaseScript)!);
								}),
								MakeInsertItem()
							}
						};
						return tvi;
					}
					x.DescendantAdded.NativeAttached.Add(i =>
					{
						Dispatcher.Invoke(() =>
						{
							Instance inst = SerializationManager.LuaDeserialize<Instance>(i[0], x.GameManager);
							TreeViewItem? parent = null;
							bool noparent = false;
							if (inst.Parent != x && inst.Parent != null)
							{
								if (Items.ContainsKey(inst.Parent))
								{
									TreeViewItem item = Items[inst.Parent];
									parent = item;
								}
								else
								{
									parent = MakeItem(inst.Parent);
									Items[inst.Parent] = parent;
								}
							}
							else
							{
								noparent = true;
							}
							{
								TreeViewItem item;
								if (!Items.ContainsKey(inst))
									Items[inst] = MakeItem(inst);
								item = Items[inst];
								if (parent != null)
								{
									if (!parent.Items.Contains(item))
									{
										parent.Items.Add(item);
									}
								}
								if (noparent)
								{
									var t = Items[inst];
									var par = (ItemsControl)t.Parent;
									if (par != null && par.Items.Contains(t))
										par.Items.Remove(t);
									explorerTree.Items.Add(t);
								}
							}
						});
					});
					x.DescendantRemoved.NativeAttached.Add(i =>
					{
						Dispatcher.Invoke(() =>
						{
							Instance inst = SerializationManager.LuaDeserialize<Instance>(i[0], x.GameManager);
							TreeViewItem? parent = null;
							if (inst.Parent != x && inst.Parent != null)
							{
								if (Items.ContainsKey(inst.Parent))
								{
									TreeViewItem item = Items[inst.Parent];
									parent = item;
								}
							}
							if (Items.ContainsKey(inst))
							{
								TreeViewItem item = Items[inst];
								if (parent != null)
								{
									if (parent.Items.Contains(item))
										parent.Items.Remove(item);
								}
							}
						});
					});
				});
				App.EditorGame.LoadDefault();
				AppManager.PlatformOpenBrowser = x =>
				{
					Dispatcher.Invoke(() =>
					{
						mw.OpenBrowserTab("Browser, opened by user code", x);
					});
				};
				AppManager.SetRenderTarget(App.EditorGame);
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
	}
}
