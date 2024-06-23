using NetBlox.Runtime;
using NetBlox.Structs;
using System.Numerics;

namespace NetBlox.Instances
{
	public class PVInstance : Instance
	{
		public bool _anchored = false;
		public Vector3 _position = Vector3.Zero;
		public Vector3 _rotation = Vector3.Zero;

		public PVInstance(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public CFrame GetPivot()
		{
			return new CFrame(_position);
		}
		[Lua([Security.Capability.None])]
		public void SetPivot(CFrame pivot)
		{
			_position = pivot.Position;
			_rotation = Vector3.Zero;
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(PVInstance) == classname) return true;
			return base.IsA(classname);
		}
	}
}
