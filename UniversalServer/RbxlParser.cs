using NetBlox.Common;
using NetBlox.Instances;
using NetBlox.Structs;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Globalization;
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
			var ic = CultureInfo.InvariantCulture;
			var slist = new SortedSet<string>();
			xml.LoadXml(File.ReadAllText(AppManager.ResolveUrlAsync(url, true).WaitAndGetResult()));

			void LoadChildren(Instance inst, XmlNode node)
			{
				var children = node.ChildNodes;
				for (int i = 0; i < children.Count; i++)
				{
					try
					{
						var child = children[i]!;
						if (child.Name != "Item") continue;

						var ins = InstanceCreator.CreateInstanceIfExists(child.Attributes!["class"]!.Value, dm.GameManager);
						if (ins == null)
						{
							slist.Add(child.Attributes!["class"]!.Value);
							continue;
						}
						var type = ins.GetType();
						var props = child.SelectNodes("Properties")![0];

						ins.Parent = inst;

						if (props != null)
						{
							for (int j = 0; j < props.ChildNodes.Count; j++)
							{
								try
								{
									var prop = props.ChildNodes[j]!;
									var attr = prop.Attributes[0];

									switch (prop.Name)
									{
										case "string" or "ProtectedString":
											SerializationManager.SetProperty(type, ins, attr.Value, prop.InnerText);
											break;
										case "double":
											SerializationManager.SetProperty(type, ins, attr.Value, double.Parse(prop.InnerText, ic));
											break;
										case "float":
											SerializationManager.SetProperty(type, ins, attr.Value, float.Parse(prop.InnerText, ic));
											break;
										case "int":
											if (attr.Value == "BrickColor")
												SerializationManager.SetProperty(type, ins, attr.Value, BrickColor.ByIndex(int.Parse(prop.InnerText, ic))!.Value);
											else
												SerializationManager.SetProperty(type, ins, attr.Value, int.Parse(prop.InnerText, ic));
											break;
										case "bool":
											SerializationManager.SetProperty(type, ins, attr.Value, bool.Parse(prop.InnerText));
											break;
										case "Vector3":
											SerializationManager.SetProperty(type, ins, attr.Value, new Vector3()
											{
												X = float.Parse(prop.ChildNodes[0].InnerText, CultureInfo.InvariantCulture),
												Y = float.Parse(prop.ChildNodes[1].InnerText, CultureInfo.InvariantCulture),
												Z = float.Parse(prop.ChildNodes[2].InnerText, CultureInfo.InvariantCulture)
											});
											break;
										case "CoordinateFrame":
											SerializationManager.SetProperty(type, ins, attr.Value, new CFrame()
											{
												Position = new Vector3()
												{
													X = float.Parse(prop.ChildNodes[0].InnerText, CultureInfo.InvariantCulture),
													Y = float.Parse(prop.ChildNodes[1].InnerText, CultureInfo.InvariantCulture),
													Z = float.Parse(prop.ChildNodes[2].InnerText, CultureInfo.InvariantCulture)
												}
											});
											break;
										case "Color3uint8":
											uint val = uint.Parse(prop.InnerText, ic);
											byte[] bs = BitConverter.GetBytes(val);
											SerializationManager.SetProperty(type, ins, attr.Value[0..^5], new Color()
											{
												R = bs[0],
												G = bs[1],
												B = bs[2],
												A = bs[3]
											});
											break;
									}
								}
								catch { }
							}
						}

						LoadChildren(ins, child);
					}
					catch { }
				}
			}

			LoadChildren(dm, xml.FirstChild);
			GC.Collect(); // hehe

			if (slist.Count > 0)
			{
				LogManager.LogWarn("While loading RBXLX file, following classes were referenced (" + slist.Count + "), yet they're not implemented!");
				var arr = slist.ToArray();
				for (int i = 0; i < slist.Count; i++)
					LogManager.LogWarn(arr[i]);
			}
		}
	}
}
