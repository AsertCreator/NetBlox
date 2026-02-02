using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;
using System.Numerics;

namespace NetBlox.Instances
{
	[Creatable]
	public class Attachment : Instance
	{
		public BasePart? ActualAdornee
		{
			get
			{
				if (Adornee == null)
					return Parent as BasePart;
				return Adornee;
			}
		}

		[Lua([Security.Capability.None])]
		public BasePart? Adornee { get; set; }
		[Lua([Security.Capability.None])]
		public CFrame CFrame { get; set; } = new CFrame();

		[Lua([Security.Capability.None])]
		public Vector3 Axis 
		{ 
			get
			{
				return Raymath.Vector3RotateByQuaternion(new Vector3(1, 0, 0), CFrame.Rotation);
			}
		}
		[Lua([Security.Capability.None])]
		public Vector3 SecondaryAxis
		{
			get
			{
				return Raymath.Vector3RotateByQuaternion(new Vector3(0, 1, 0), CFrame.Rotation);
			}
		}
		[Lua([Security.Capability.None])]
		public CFrame WorldCFrame
		{
			get
			{
				var result = CFrame;
				if (ActualAdornee != null)
				{
					result.Position = Raymath.Vector3RotateByQuaternion(CFrame.Position, CFrame.Rotation * ActualAdornee.CFrame.Rotation);
					result.Position += ActualAdornee.CFrame.Position;
					result.Rotation = CFrame.Rotation * ActualAdornee.CFrame.Rotation;
				}
				return result;
			}
		}
		[Lua([Security.Capability.None])]
		public Vector3 WorldAxis
		{
			get
			{
				return Raymath.Vector3RotateByQuaternion(new Vector3(1, 0, 0), WorldCFrame.Rotation);
			}
		}
		[Lua([Security.Capability.None])]
		public Vector3 WorldSecondaryAxis
		{
			get
			{
				return Raymath.Vector3RotateByQuaternion(new Vector3(0, 1, 0), WorldCFrame.Rotation);
			}
		}

		public Attachment(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Attachment) == classname) return true;
			return base.IsA(classname);
		}
	}
}
