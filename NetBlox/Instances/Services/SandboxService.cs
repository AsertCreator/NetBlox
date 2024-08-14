using NetBlox.Runtime;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NetBlox.Instances.Services
{
	public class SandboxService : Instance, I3DRenderable
	{
		public SandboxService(GameManager ins) : base(ins) 
		{
			ins.RenderManager.CurrentHint = "You're currently playing NetBlox Sandbox";
		}

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(SandboxService) == classname) return true;
			return base.IsA(classname);
		}
		public void Render()
		{
			int mx = Raylib.GetMouseX();
			int my = Raylib.GetMouseY();
			var mp = Raylib.GetMouseRay(new System.Numerics.Vector2()
			{
				X = mx, Y = my
			}, 
			GameManager.RenderManager.MainCamera);
			var works = Root.GetService<Workspace>(true);
			if (works != null)
			{
				mp.Direction *= 30;

				var res = works.Raycast(new()
				{
					From = mp.Position,
					To = mp.Position + mp.Direction,
					MaxDistance = 500
				});

				if (res.Part != null)
				{
					Raylib.DrawCube(res.Part.Position, res.Part.Size.X, res.Part.Size.Y, res.Part.Size.Z, Color.Red);
					if (Raylib.IsMouseButtonPressed(MouseButton.Left))
					{
						RenderManager.LoadSound("rbxasset://sounds/boom.wav", x => GameManager.RenderManager.PlaySound(x));
						res.Part.Destroy();
					}
				}
			}
		}
	}
}
