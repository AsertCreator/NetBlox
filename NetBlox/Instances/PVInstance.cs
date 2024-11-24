using NetBlox.Runtime;
using NetBlox.Structs;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace NetBlox.Instances
{
	public class PVInstance : Instance
	{
		public bool _anchored = false;
		public Vector3 _position 
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _pivot.Position;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set 
			{
				_pivot.Position = value;
				if (this is BasePart bp)
				{
					if (bp.LocalLighing != null && bp.LocalLighing.SunLocality)
						bp.RenderCache.DirtyCounter = 6;
				}
			} 
		}
		public Vector3 _rotation
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _pivot.Rotation;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set 
			{
				_pivot.Rotation = value;
				if (this is BasePart bp)
					bp.RenderCache.DirtyCounter = 6;
			}
		}
		public Vector3 _lastposition = Vector3.Zero;
		public Vector3 _lastrotation = Vector3.Zero;
		public CFrame _pivot;
		public CFrame _pivotOffset;
		[Lua([Security.Capability.None])]
		public CFrame PivotOffset
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _pivotOffset;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _pivotOffset = value; 
		}
		[Lua([Security.Capability.None])]
		public CFrame CFrame
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => GetPivot();
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => SetPivot(value); 
		}

		public PVInstance(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
