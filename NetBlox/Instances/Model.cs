using NetBlox.Runtime;
using NetBlox.Structs;
using System.Numerics;

namespace NetBlox.Instances
{
	[Creatable]
	public class Model : PVInstance
	{
		[Lua([Security.Capability.None])]
		public BasePart? PrimaryPart
		{
			get => _primarypart;
			set
			{
				if (!value.IsDescendantOf(this))
					throw new Exception("Can't set PrimaryPart property of a Model to a part outside of the model");
				_primarypart = value;
			}
		}
		private BasePart? _primarypart = null;
		private BasePart? _lastpivotpart = null;
		private float _scale = 1;

		public Model(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Model) == classname) return true;
			return base.IsA(classname);
		}
		[Lua([Security.Capability.None])]
		public float GetScale() => _scale;
		[Lua([Security.Capability.None])]
		public void ScaleTo(float sc) => throw new NotImplementedException();
		[Lua([Security.Capability.None])]
		public void SetIdentityOrientation()
		{
			if (Children.Count == 0)
				return;
			BasePart pivotpart = GetPivotPart();
			Vector3 rotationpivot = pivotpart._position;

			// do something
		}
		[Lua([Security.Capability.None])]
		public void MoveTo(Vector3 pos)
		{
			if (Children.Count == 0)
				return;
			SetIdentityOrientation();
			TranslateBy(pos - _lastpivotpart.Position);
		}
		[Lua([Security.Capability.None])]
		public void TranslateBy(Vector3 pos)
		{
			if (Children.Count == 0)
				return;
			Instance[] descendants = GetDescendants();
			for (int i = 0; i < descendants.Length; i++)
			{
				if (descendants[i] is BasePart part)
					part.Position += pos;
			}
		}
		[Lua([Security.Capability.None])]
		public BasePart? GetPivotPart()
		{
			if (PrimaryPart != null)
			{
				_lastpivotpart = PrimaryPart;
				return PrimaryPart;
			}
			Instance[] descendants = GetDescendants();
			for (int i = 0; i < descendants.Length; i++)
			{
				if (descendants[i] is BasePart part)
				{
					_lastpivotpart = part;
					return part;
				}
			}
			return null;
		}
		[Lua([Security.Capability.None])]
		public void BreakJoints()
		{
			Instance[] descendants = GetDescendants();
			for (int i = 0; i < descendants.Length; i++)
			{
				if (descendants[i] is Weld weld)
				{
					weld.Destroy();
				}
			}
		}
		public override void PivotTo(CFrame pivot) => throw new NotImplementedException();
	}
}
