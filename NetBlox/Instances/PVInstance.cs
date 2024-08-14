using NetBlox.Runtime;
using NetBlox.Structs;
using System.Numerics;

namespace NetBlox.Instances
{
	public class PVInstance : Instance
	{
		public bool _anchored = false;
		public Vector3 _position { get => _pivot.Position; set => _pivot.Position = value; }
		public Vector3 _rotation { get => _pivot.Rotation; set => _pivot.Rotation = value; }
		public Vector3 _lastposition = Vector3.Zero;
		public Vector3 _lastrotation = Vector3.Zero;
		public CFrame _pivot;
		public CFrame _pivotOffset;
		[Lua([Security.Capability.None])]
		public CFrame PivotOffset { get => _pivotOffset; set => _pivotOffset = value; }
		[Lua([Security.Capability.None])]
		public CFrame CFrame { get => GetPivot(); set => SetPivot(value); }

		public PVInstance(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public virtual CFrame GetPivot() => _pivot * _pivotOffset;
		[Lua([Security.Capability.None])]
		public virtual void SetPivot(CFrame pivot) { }
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(PVInstance) == classname) return true;
			return base.IsA(classname);
		}
	}
}
