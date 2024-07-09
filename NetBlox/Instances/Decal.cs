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

		public Decal(GameManager ins) : base(ins) { }

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
			base.RenderUI();
			if (ActualTexture != null)
				RenderUtils.DrawCubeTextureRec((Texture2D)ActualTexture, bp.Position, bp.Rotation,
					bp.Size.X + 0.002f, bp.Size.Y + 0.002f, bp.Size.Z + 0.002f, Color.White, Face); // just so it could render
		}
	}
}
