using NetBlox.Runtime;
using NetBlox.Instances;
using System.Numerics;
using Qu3e;

namespace NetBlox.Instances.Services
{
	public class Workspace : Instance
	{
		[Lua([Security.Capability.None])]
		public Instance? MainCamera { get; set; }
		[Lua([Security.Capability.None])]
		public Vector3 Gravity { get; set; } = new Vector3(0, -9.8f, 0);
		public Scene Scene;

		public Workspace(GameManager ins) : base(ins) 
		{ 
			Scene = new(1 / AppManager.PreferredFPS, Gravity, 10);
		}

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Workspace) == classname) return true;
			return base.IsA(classname);
		}
		[Lua([Security.Capability.None])]
		public void ZoomToExtents()
		{
			GameManager.RenderManager.MainCamera.position = new Vector3(50, 40, 0);
			GameManager.RenderManager.MainCamera.target = Vector3.Zero;
		}
	}
}
