using NetBlox.Runtime;
using NetBlox.Instances;
using System.Numerics;

namespace NetBlox.Instances.Services
{
	public class Workspace : Instance
	{
		[Lua([Security.Capability.None])]
		public Instance? MainCamera { get; set; }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Workspace) == classname) return true;
			return base.IsA(classname);
		}
		[Lua([Security.Capability.None])]
		public void ZoomToExtents()
		{
			RenderManager.MainCamera.Position = new Vector3(50, 40, 0);
			RenderManager.MainCamera.Target = Vector3.Zero;
		}
	}
}
