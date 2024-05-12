using MoonSharp.Interpreter;
using NetBlox.Instances.Services;
using NetBlox.Runtime;

namespace NetBlox.Instances
{
	public class DataModel : Instance
	{
		[Lua([Security.Capability.None])]
		public string TestString { get; set; } = "Test";
		[Lua([Security.Capability.None])]
		public int PreferredFPS { get => RenderManager.PreferredFPS; set => RenderManager.SetPreferredFPS(value); }
		public Dictionary<Scripts.ModuleScript, Table> LoadedModules = new();
		public Script MainEnv = null!;

        public T GetService<T>(bool allownull = false) where T : Instance, new()
        {
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i] is T)
                    return (T)Children[i];
            }
			if (!allownull)
			{
				var serv = new T();
				serv.Parent = this;
				return serv;
			}
			return null!;
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
        [Lua([Security.Capability.CoreSecurity])]
        public void AddCrossDataModelInstance(Instance ins)
        {
			GameManager.CrossDataModelInstances.Add(ins);
        }
        [Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(DataModel) == classname) return true;
			return base.IsA(classname);
		}
	}
}
