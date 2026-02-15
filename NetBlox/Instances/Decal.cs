using NetBlox.Instances.Services;
using NetBlox.Runtime;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NetBlox.Instances
{
	[Creatable]
	public class Decal : Instance, I3DRenderable
	{
		[Lua([Security.Capability.None])]
		public string Texture
		{
			get => texture;
			set
			{
				texture = value;
				if (GameManager.RenderManager != null)
					RenderManager.LoadTexture(value, x =>
					{
						ActualTexture = x;
					});
			}
		}
		[Lua([Security.Capability.None])]
		public Faces Face { get; set; } = Faces.Front;
		public Texture2D? ActualTexture;
		private string texture = "";

		public Decal(GameManager ins) : base(ins) 
		{
			GameManager.RenderManager.Visibles3DGrade0.Add(this);
		}

		public override void Destroy()
		{
			GameManager.RenderManager.Visibles3DGrade0.Remove(this);
			base.Destroy();
		}

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Folder) == classname) return true;
			return base.IsA(classname);
		}
		public void Render()
		{
			if (Parent == null) return;
			if (Parent is not BasePart) return;

			var bp = (BasePart)Parent;

			if (!bp.IsDescendantOf(Root.GetService<Workspace>(false)))
				return;

			if (ActualTexture != null) 
			{ 
				RenderUtils.DrawCubeTextureRec((Texture2D)ActualTexture, bp.Position, bp._rotation,
					bp.Size.X + 0.002f, bp.Size.Y + 0.002f, bp.Size.Z + 0.002f, Color.White, Face); // just so it could render
			}
		}
	}
}
