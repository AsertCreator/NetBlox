using MoonSharp.Interpreter;
using NetBlox.Structs;

namespace NetBlox.Runtime
{
	// it was supposed to handle all types in lua, but im lazy so no
	public static class LuaTypes
	{
		public static List<LuaEnum> AllEnums = [];

		static LuaTypes()
		{
			AddEnum(typeof(Faces));
			AddEnum(typeof(Shape));
			AddEnum(typeof(SurfaceType));
		}
		public static void ImportAll(Table table)
		{
			var enumtable = new Table(table.OwnerScript);
			table["Enum"] = enumtable;

			for (int i = 0; i < AllEnums.Count; i++)
			{
				var enu = AllEnums[i];
				var itable = new Table(table.OwnerScript);
				enumtable[enu.Name] = itable;

				for (int j = 0; j < enu.ValueMap.Count; j++)
				{
					var kvp = enu.ValueMap.ElementAt(j);
					itable[kvp.Key] = kvp.Value;
				}
			}
		}
		public static void AddEnum(Type enu)
		{
			var names = enu.GetEnumNames();
			var luae = new LuaEnum();
			var i = 0;

			luae.Name = enu.Name;
			luae.OriginalType = enu;
			luae.ValueMap = enu.GetEnumValues().Cast<int>().ToDictionary(x => names[i++]);

			AllEnums.Add(luae);
		}
		public class LuaEnum
		{
			public string Name;
			public Type OriginalType;
			public Dictionary<string, int> ValueMap;
		}
	}
}
