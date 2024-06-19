using ImGuiNET;
using NetBlox.Instances;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using Raylib_CsLo;
using rlImGui_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NetBlox.Studio
{
	public class EditorManager
	{
		public bool ShowLuaExecutor = false;
		public bool ShowInstanceTree = true;
		public bool ShowProperties = true;
		public bool ShowOutput = false;
		public string CurrentLuaCode = "";
		public Instance? Selection = null;
		public Project? CurrentProject;
		public Dictionary<BaseScript, bool> ShowedScripts = [];

		public EditorManager(RenderManager rm) 
		{
			CurrentProject = new(rm.GameManager.CurrentRoot, false, "");
			Task.Run(() =>
			{
				Console.WriteLine("NetBlox Console is running (enter Lua code to run it)");
				while (!rm.GameManager.ShuttingDown)
				{
					Console.Write(">>> ");
					var c = Console.ReadLine();
					LuaRuntime.Execute(c, 8, rm.GameManager, null);
				}
			});
			rm.PostRender = () =>
			{
				rlImGui.Begin();

				var v = ImGui.GetMainViewport();
				var flags = ImGuiWindowFlags.NoBringToFrontOnFocus |
					ImGuiWindowFlags.NoNavFocus |
					ImGuiWindowFlags.NoDocking |
					ImGuiWindowFlags.NoTitleBar |
					ImGuiWindowFlags.NoResize |
					ImGuiWindowFlags.NoMove |
					ImGuiWindowFlags.NoCollapse |
					ImGuiWindowFlags.MenuBar |
					ImGuiWindowFlags.NoBackground;
				var pv = ImGui.GetStyle().WindowPadding;

				ImGui.Begin("#root", flags);
				ImGui.SetWindowPos(-pv);
				ImGui.SetWindowSize(new Vector2(rm.ScreenSizeX, rm.ScreenSizeY) + pv * 2);

				ImGui.DockSpace(v.ID, new(0.0f, 0.0f), ImGuiDockNodeFlags.PassthruCentralNode);
				ImGui.BeginMainMenuBar();
				if (ImGui.BeginMenu("NetBlox"))
				{
					if (ImGui.MenuItem("Begin playtest"))
					{
						// do smth
					}
					if (ImGui.MenuItem("Save"))
						CurrentProject.SaveProject(false, x => Raylib.SetWindowTitle("netblox studio - " + x));
					if (ImGui.MenuItem("Save as"))
						CurrentProject.SaveProject(true, x => Raylib.SetWindowTitle("netblox studio - " + x));
					if (ImGui.MenuItem("Take screenshot"))
						rm.GameManager.CurrentRoot.GetService<CoreGui>().TakeScreenshot();
					if (ImGui.MenuItem("Exit"))
					{
						AppManager.Shutdown();
						return;
					}
					ImGui.EndMenu();
				}
				if (ImGui.BeginMenu("View"))
				{
					if (ImGui.MenuItem(ShowLuaExecutor ? "Close Lua executor" : "Open Lua executor"))
						ShowLuaExecutor = !ShowLuaExecutor;
					if (ImGui.MenuItem(ShowOutput ? "Close output" : "Show output"))
						ShowOutput = !ShowOutput;
					if (ImGui.MenuItem(ShowInstanceTree ? "Close explorer" : "Show explorer"))
						ShowInstanceTree = !ShowInstanceTree;
					ImGui.EndMenu();
				}
				if (ImGui.BeginMenu("Help"))
				{
					ImGui.EndMenu();
				}
				if (ImGui.BeginMenu("Debug"))
				{
					if (ImGui.MenuItem("Switch client network identity", "", rm.GameManager.NetworkManager.IsClient))
						rm.GameManager.NetworkManager.IsClient = !rm.GameManager.NetworkManager.IsClient;
					if (ImGui.MenuItem("Switch server network identity", "", rm.GameManager.NetworkManager.IsServer))
						rm.GameManager.NetworkManager.IsServer = !rm.GameManager.NetworkManager.IsServer;
					ImGui.EndMenu();
				}
				ImGui.EndMainMenuBar();

				ImGui.End();
				if (ShowInstanceTree)
				{
					ImGui.Begin("Explorer");
					void Node(Instance ins, int s = 0)
					{
						string msg = ins.Name + " - " + ins.ClassName;
						bool open = ImGui.TreeNode(msg);
						if (ImGui.BeginPopupContextItem())
						{
							if (s != 0 && ImGui.MenuItem("Destroy"))
							{
								ins.Destroy();
							}
							if (ins is BaseScript && ImGui.MenuItem("Edit script"))
							{
								var bs = ins as BaseScript;
								if (ShowedScripts.ContainsKey(bs!))
									ShowedScripts[bs!] = !ShowedScripts[bs!];
								else
									ShowedScripts[bs!] = true;
							}
							if (ImGui.BeginMenu("Insert object"))
							{
								for (int i = 0; i < InstanceCreator.CreatableInstanceTypes.Length; i++)
								{
									var t = InstanceCreator.CreatableInstanceTypes[i];
									if (ImGui.MenuItem(t.Name))
									{
										var inst = InstanceCreator.CreateInstance(t.Name, rm.GameManager);
										inst.Parent = ins;
									}
								}
								ImGui.EndMenu();
							}
							ImGui.EndPopup();
						}
						if (open)
						{
							for (int i = 0; i < ins.Children.Count; i++)
							{
								Node(ins.Children[i], s + 1);
							}
							ImGui.TreePop();
						}
					}
					if (rm.GameManager.CurrentRoot != null)
					{
						Node(rm.GameManager.CurrentRoot.GetService<Workspace>());
						Node(rm.GameManager.CurrentRoot.GetService<Lighting>());
						Node(rm.GameManager.CurrentRoot.GetService<Players>());
						Node(rm.GameManager.CurrentRoot.GetService<ServerStorage>());
						Node(rm.GameManager.CurrentRoot.GetService<ReplicatedFirst>());
						Node(rm.GameManager.CurrentRoot.GetService<ReplicatedStorage>());
						Node(rm.GameManager.CurrentRoot.GetService<StarterPack>());
						Node(rm.GameManager.CurrentRoot.GetService<StarterGui>());
					}

					ImGui.End();
				}
				foreach (var kvp in ShowedScripts)
				{
					if (kvp.Value)
					{
						string s = kvp.Key.Source;
						ImGui.SetWindowSize(new(400, 500));
						ImGui.Begin("Script EditorManager - " + kvp.Key.GetFullName());
						if (!kvp.Key.IsA("CoreScript"))
							ImGui.InputTextMultiline("", ref s, (uint)kvp.Key.Source.Length * 2 + 1024, ImGui.GetWindowSize());
						else
						{
							ImGui.Text("Cannot modify CoreScripts");
							ImGui.InputTextMultiline("", ref s, (uint)kvp.Key.Source.Length + 1024, ImGui.GetWindowSize(), ImGuiInputTextFlags.ReadOnly);
						}
						kvp.Key.Source = s;
						ImGui.End();
					}
				}
				if (ShowLuaExecutor)
				{
					ImGui.Begin("Lua executor");
					ImGui.SetWindowSize(new Vector2(400, 300));
					ImGui.InputTextMultiline("", ref CurrentLuaCode, 256 * 1024, new Vector2(400 - 12, 300 - 55));
					if (ImGui.Button("Execute"))
					{
						LuaRuntime.Execute(CurrentLuaCode, 4, rm.GameManager, null);
					}
					ImGui.End();
				}
				if (ShowOutput)
				{
					string log = LogManager.Log.ToString();
					ImGui.Begin("Output");
					ImGui.InputTextMultiline("", ref log, (uint)(log.Length + 50), ImGui.GetWindowSize(), ImGuiInputTextFlags.ReadOnly);
					ImGui.End();
				}
				if (ShowProperties)
				{
					ImGui.Begin("Properties");

					ImGui.End();
				}

				rlImGui.End();
			};
		}
	}
}
