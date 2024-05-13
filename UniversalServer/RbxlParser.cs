using NetBlox.Instances;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace NetBlox.Server
{
	public static class RbxlParser
	{
		public static void Load(string url, DataModel dm)
		{
			var xml = new XmlDocument();
			xml.LoadXml(File.ReadAllText(url));

			void LoadChildren(Instance inst, XmlNode node)
			{
				var children = node.ChildNodes;
				for (int i = 0; i < children.Count; i++)
				{
					try
					{
						var child = children[i]!;
						if (child.Name != "Item") continue;

						var ins = InstanceCreator.CreateInstance(child.Attributes!["class"]!.Value);
						var type = ins.GetType();
						var props = node.SelectSingleNode("Properties");

						if (props != null)
						{
							for (int j = 0; j < props.ChildNodes.Count; j++)
							{
								var prop = props.ChildNodes[j]!;
								var attr = prop.Attributes[0];

								switch (prop.Name)
								{
									case "string" or "ProtectedString":
										SerializationManager.SetProperty(type, ins, attr.Value, prop.InnerText);
										break;
									case "double":
										SerializationManager.SetProperty(type, ins, attr.Value, double.Parse(prop.InnerText));
										break;
									case "float":
										SerializationManager.SetProperty(type, ins, attr.Value, float.Parse(prop.InnerText));
										break;
									case "int":
										SerializationManager.SetProperty(type, ins, attr.Value, int.Parse(prop.InnerText));
										break;
									case "bool":
										SerializationManager.SetProperty(type, ins, attr.Value, bool.Parse(prop.InnerText));
										break;
									case "Vector3":
										SerializationManager.SetProperty(type, ins, attr.Value, new Vector3()
										{
											X = float.Parse(prop.ChildNodes[0].InnerText),
											Y = float.Parse(prop.ChildNodes[1].InnerText),
											Z = float.Parse(prop.ChildNodes[2].InnerText)
										});
										break;
								}
							}
						}

						ins.Parent = inst;

						LoadChildren(ins, child);
					}
					catch { }
				}
			}

			LoadChildren(dm, xml.FirstChild);
		}
	}
}
