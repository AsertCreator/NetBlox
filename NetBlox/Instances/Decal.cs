using NetBlox.Runtime;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NetBlox.Instances
{
	[Creatable]
	public class Decal : Instance
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

		public Decal(GameManager ins) : base(ins) { Debugger.Break(); }

		public override void RenderUI()
		{
			if (Parent == null) return;
			if (Parent is not BasePart) return;
			var bp = (BasePart)Parent;
			base.RenderUI();
			if (ActualTexture != null)
				RenderUtils.DrawCubeTextureRec((Texture2D)ActualTexture, bp.Position, bp.Rotation, 
					bp.Size.X + float.Epsilon, bp.Size.Y + float.Epsilon, bp.Size.Z + float.Epsilon, Color.White, Face); // just so it could render
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Folder) == classname) return true;
			return base.IsA(classname);
		}
	}
}
