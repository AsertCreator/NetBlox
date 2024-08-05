using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace NBApiDumper
{
	public static class Program
	{
		public static int Main(string[] args)
		{
			if (args.Length != 1) 
			{
				Console.WriteLine("expected client's .dll file to be passed!");
				return 1;
			}
			var assm = Assembly.LoadFrom(args[0]);
			var inst = assm.GetType("NetBlox.Instances.Instance");
			var typs = assm.GetTypes().OrderBy(x => x.Name).ToArray();
			var outdir = Directory.CreateDirectory("./docs/");
			var trueinsts = new List<Type>();

			if (outdir.GetFiles().Length > 0) 
			{
				outdir.Delete(true);
				outdir = Directory.CreateDirectory("./docs/");
			}

			for (int i = 0; i < typs.Length; i++)
			{
				var type = typs[i];
				if (type.IsAssignableTo(inst))
				{
					Console.WriteLine($"processing class {type.Name} : {type.BaseType!.Name}...");
					var sb = new StringBuilder();
					var fi = new FileInfo(Path.Combine(outdir.FullName, type.Name + ".md"));
					var fs = fi.Create();

					var luameths = type.GetMethods().Where(y => 
					{
						var attrbs = y.GetCustomAttributes();

						foreach (var attr in attrbs)
							if (attr.GetType().Name == "LuaAttribute")
								return true;

						return false;
					}).ToArray();

					var luaprops = type.GetProperties().Where(y =>
					{
						var attrbs = y.GetCustomAttributes();

						foreach (var attr in attrbs)
							if (attr.GetType().Name == "LuaAttribute")
								return true;

						return false;
					}).ToArray();

					sb.AppendLine("# " + type.Name);
					sb.AppendLine("\n[Go back to home page](index.md)\n");
					if (type.Name == "Instance")
						sb.AppendLine($"`{type.Name}`, is the class hierarchy root, has {luameths.Length} methods and {luaprops.Length} properties");
					else
						sb.AppendLine($"`{type.Name}` extends from [`{type.BaseType.Name}`]({type.BaseType.Name}.md), has {luameths.Length} methods and {luaprops.Length} properties");

					sb.AppendLine("### Methods");
					sb.AppendLine("| Name | Arguments | Required capabilities |");
					sb.AppendLine("|------|-----------|-----------------------|");

					for (int j = 0; j < luameths.Length; j++)
					{
						var attrbs = luameths[j].GetCustomAttributes();
						Attribute? luattr = null;

						foreach (var attr in attrbs)
							if (attr.GetType().Name == "LuaAttribute")
								luattr = attr;

						var sec = (Array)luattr!.GetType().GetProperty("Capabilities")!.GetValue(luattr);
						var sectype = sec!.GetType().GetElementType();
						var capstr = sectype!.GetEnumName(sec.GetValue(0)!);

						for (int k = 1; k < sec.Length; k++)
							capstr += ", " + sectype.GetEnumName(sec.GetValue(k)!);

						sb.AppendLine($"`{luameths[j].Name}` | `{string.Join(", ", from x in luameths[j].GetParameters() select x.ParameterType.Name)}` " +
							$"| `{capstr}` |");
					}

					fs.Write(Encoding.UTF8.GetBytes(sb.ToString()));
					fs.Flush();
					fs.Dispose();

					trueinsts.Add(type);
				}
			}

			{			
				var sb = new StringBuilder();
				var fi = new FileInfo(Path.Combine(outdir.FullName, "index.md"));
				var fs = fi.Create();

				sb.AppendLine("# NetBlox API Dump");
				sb.AppendLine($"The API Dump lists every single `Instance` accessible from Lua code, including their methods " +
					$"properties and events. There's total of {trueinsts.Count} classes. There are links to every page: ");

				for (int i = 0; i < trueinsts.Count; i++)
				{
					var type = trueinsts[i];

					sb.AppendLine($"- [{type.Name}]({type.Name}.md)");
				}

				fs.Write(Encoding.UTF8.GetBytes(sb.ToString()));
				fs.Flush();
				fs.Dispose();
			}

			Console.WriteLine("markdown docs created at " + outdir.FullName);
			Process.Start(new ProcessStartInfo() { FileName = outdir.FullName, UseShellExecute = true });

			return 0;
		}
	}
}
