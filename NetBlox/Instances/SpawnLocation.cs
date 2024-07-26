using NetBlox.Instances.Services;
using NetBlox.Runtime;
#if STUDIO
using NetBlox.Studio;
#endif
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances
{
	[Creatable]
	public class SpawnLocation : Part
	{
		public SpawnLocation(GameManager ins) : base(ins) { }

#if STUDIO
		[StudioSpawn]
		public static SpawnLocation Fabricate(GameManager gm)
		{
			SpawnLocation sl = new(gm);
			Decal dc = new(gm);
			sl.Size = new System.Numerics.Vector3(8, 1, 8);
			dc.Texture = "rbxasset://textures/spawn.png";
			dc.Parent = sl;
			dc.Face = Faces.Top;
			return sl;
		}
#endif

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(SpawnLocation) == classname) return true;
			return base.IsA(classname);
		}
		public override void Process()
		{
			base.Process();
			Workspace? works = Root.GetService<Workspace>(true);
			if (works != null)
				works.SpawnLocation = this; // a rolling mechanism
		}
	}
}
