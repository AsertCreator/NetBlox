using Raylib_cs;
using NetBlox.Runtime;
using NetBlox.Structs;
using System.Numerics;

namespace NetBlox.Instances
{
	[Creatable]
	public class Part : BasePart
	{
		[Lua([Security.Capability.None])]
		public Shape Shape { get; set; } = Shape.Block;

		public Part(GameManager ins) : base(ins) { }

		public unsafe override void Render()
		{
			switch (Shape)
			{
				case Shape.Ball:
					break;
				case Shape.Block:
					var st = GameManager.RenderManager.StudTexture;
					var mesh = Raylib.GenMeshCube(Size.X, Size.Y, Size.Z);
					var model = Raylib.LoadModelFromMesh(mesh);
					model.Materials[0].Maps[(int)MaterialMapIndex.Diffuse].Texture = st;

					Raylib.DrawModel(model, Position, 1, Color);

					Raylib.UnloadModel(model);
					break;
				case Shape.Cylinder:
					break;
				case Shape.Wedge:
					break;
				case Shape.CornerWedge:
					break;
			}

			base.Render();
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Part) == classname) return true;
			return base.IsA(classname);
		}
	}
}
