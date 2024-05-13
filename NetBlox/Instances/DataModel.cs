using MoonSharp.Interpreter;
using NetBlox.Instances.Services;
using NetBlox.Runtime;

namespace NetBlox.Instances
{
	public class DataModel : Instance
	{
		public Dictionary<Scripts.ModuleScript, Table> LoadedModules = new();
		public Script MainEnv = null!;

		public DataModel(GameManager ins) : base(ins) { }

		public T GetService<T>(bool allownull = false) where T : Instance
		{
			for (int i = 0; i < Children.Count; i++)
			{
				if (Children[i] is T)
					return (T)Children[i];
			}
			if (!allownull)
			{
				var serv = (T)Activator.CreateInstance(typeof(T), GameManager);
				serv.Parent = this;
                return serv;
			}
			return null!;
		}

		[Lua([Security.Capability.None])]
		public Instance GetService(string sn)
		{
			for (int i = 0; i < Children.Count; i++)
			{
				if (Children[i].ClassName == sn)
					return Children[i];
			}
			var serv = InstanceCreator.CreateInstance(sn);
			serv.Parent = this;
			return serv;
		}
		[Lua([Security.Capability.None])]
		public bool IsLoaded()
		{
			return true;
		}
		[Lua([Security.Capability.None])]
		public void Shutdown()
		{
			GameManager.Shutdown();
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(DataModel) == classname) return true;
			return base.IsA(classname);
		}
	}
}
