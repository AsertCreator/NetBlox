using NetBlox.Instances;
using NetBlox.Instances.Services;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace NetBlox.Studio
{
	public class Project
	{
		public DataModel DataModel;
		public string FilePath = "";

		public Project(DataModel dm, bool load, string path)
		{
			DataModel = dm;
			FilePath = path;
			if (load)
				LoadProject(path);
		}
		public unsafe void SaveProject(bool saveas, Action<string> cb)
		{
			var xml = new XmlDocument();
			var project = xml.CreateElement("Project");
			xml.AppendChild(project);

			void AddInstance(Instance ins, XmlNode n)
			{
				var el = xml.CreateElement("Instance");
				var prs = SerializationManager.GetAccessibleProperties(ins);
				var chs = ins.GetChildren();

				el.SetAttribute("class", ins.ClassName);
				n.AppendChild(el);

				for (int i = 0; i < prs.Length; i++)
				{
					if (prs[i] == "ClassName" || prs[i] == "Parent") continue;
					var pn = xml.CreateElement("Property");
					pn.SetAttribute("name", prs[i]);
					switch (SerializationManager.GetSerializationType(ins, prs[i]))
					{
						case SerializationType.String: 
							pn.SetAttribute("type", "string"); 
							pn.InnerText = (string)SerializationManager.GetProperty(ins, prs[i]);
							el.AppendChild(pn); 
							break;
						case SerializationType.Enum:
							pn.SetAttribute("type", "enum");
							pn.InnerText = (Convert.ToInt32(SerializationManager.GetProperty(ins, prs[i]))).ToString();
							el.AppendChild(pn);
							break;
						case SerializationType.Int32: 
							pn.SetAttribute("type", "int32");
							pn.InnerText = ((int)SerializationManager.GetProperty(ins, prs[i])).ToString();
							el.AppendChild(pn); 
							break;
						case SerializationType.Int64: 
							pn.SetAttribute("type", "int64");
							pn.InnerText = ((long)SerializationManager.GetProperty(ins, prs[i])).ToString();
							el.AppendChild(pn); 
							break;
						case SerializationType.Single: 
							pn.SetAttribute("type", "single");
							pn.InnerText = ((float)SerializationManager.GetProperty(ins, prs[i])).ToString();
							el.AppendChild(pn); 
							break;
						case SerializationType.Double: 
							pn.SetAttribute("type", "double");
							pn.InnerText = ((double)SerializationManager.GetProperty(ins, prs[i])).ToString();
							el.AppendChild(pn); 
							break;
						case SerializationType.True: 
							pn.SetAttribute("type", "true"); 
							el.AppendChild(pn); 
							break;
						case SerializationType.False: 
							pn.SetAttribute("type", "false"); 
							el.AppendChild(pn); 
							break;
						case SerializationType.Vector3:
							pn.SetAttribute("type", "v3");
							pn.InnerText = SerializationManager.Serialize((Vector3)SerializationManager.GetProperty(ins, prs[i]));
							el.AppendChild(pn);
							break;
						case SerializationType.Color3:
							pn.SetAttribute("type", "c3");
							pn.InnerText = SerializationManager.Serialize((Color)SerializationManager.GetProperty(ins, prs[i]));
							el.AppendChild(pn);
							break;
					}
				}
				for (int i = 0; i < chs.Length; i++)
					AddInstance(chs[i], el);
			}

			AddInstance(DataModel.GetService<Workspace>(), project);
			AddInstance(DataModel.GetService<ReplicatedFirst>(), project);
			AddInstance(DataModel.GetService<ReplicatedStorage>(), project);
			AddInstance(DataModel.GetService<ServerStorage>(), project);
			AddInstance(DataModel.GetService<StarterPack>(), project);
			AddInstance(DataModel.GetService<StarterGui>(), project);

			if (string.IsNullOrWhiteSpace(FilePath) || saveas)
			{
				Thread th = new(() =>
				{
					var nw = new NativeWindow();
					using var ofd = new SaveFileDialog();

					nw.AssignHandle(new nint(Raylib.GetWindowHandle()));
					ofd.Title = "Select destination RBXL file";
					ofd.Filter = "Project files (.rbxl)|*.rbxl";

					if (ofd.ShowDialog(nw) == DialogResult.OK)
					{
						FilePath = ofd.FileName;
						File.WriteAllText(ofd.FileName, XDocument.Parse(xml.InnerXml).ToString());
						cb(FilePath);
					}
				});
				th.SetApartmentState(ApartmentState.STA);
				th.Start();
			}
			else
			{
				File.WriteAllText(FilePath, XDocument.Parse(xml.InnerXml).ToString());
				cb(FilePath);
			}
		}
		public DataModel LoadProject(string path)
		{
			var d = File.ReadAllText(path);
			var xml = new XmlDocument();
			xml.LoadXml(d);

			LogManager.LogInfo("Loading project from XML...");

			var root = xml.GetElementsByTagName("Project")[0];
			Instance ParseInstance(XmlNode node)
			{
				var ins = InstanceCreator.CreateInstance(node.Attributes["class"].InnerText, DataModel.GameManager);
				var prs = node.SelectNodes("Property");
				var els = node.SelectNodes("Instance");
				for (int i = 0; i < prs.Count; i++)
				{
					var prp = prs[i];
					switch (prp.Attributes["type"].InnerText)
					{
						case "string":
							SerializationManager.SetProperty(ins, prp.Attributes["name"].InnerText, prp.InnerText);
							break;
						case "enum":
							SerializationManager.SetProperty(ins, prp.Attributes["name"].InnerText, int.Parse(prp.InnerText));
							break;
						case "int32":
							SerializationManager.SetProperty(ins, prp.Attributes["name"].InnerText, int.Parse(prp.InnerText));
							break;
						case "int64":
							SerializationManager.SetProperty(ins, prp.Attributes["name"].InnerText, long.Parse(prp.InnerText));
							break;
						case "single":
							SerializationManager.SetProperty(ins, prp.Attributes["name"].InnerText, float.Parse(prp.InnerText));
							break;
						case "double":
							SerializationManager.SetProperty(ins, prp.Attributes["name"].InnerText, double.Parse(prp.InnerText));
							break;
						case "true":
							SerializationManager.SetProperty(ins, prp.Attributes["name"].InnerText, true);
							break;
						case "false":
							SerializationManager.SetProperty(ins, prp.Attributes["name"].InnerText, false);
							break;
						case "v3":
							SerializationManager.SetProperty(ins, prp.Attributes["name"].InnerText, SerializationManager.Deserialize<Vector3>(prp.InnerText));
							break;
						case "c3":
							SerializationManager.SetProperty(ins, prp.Attributes["name"].InnerText, SerializationManager.Deserialize<Color>(prp.InnerText));
							break;
					}
				}
				for (int i = 0; i < els.Count; i++)
				{
					var chd = ParseInstance(els[i]);
					chd.Parent = ins;
				}
				return ins;
			}

			var els = xml.SelectNodes("Instance");
			for (int i = 0; i < els.Count; i++)
			{
				var chd = ParseInstance(els[i]);
				chd.Parent = DataModel;
			}

			return DataModel;
		}
	}
}
