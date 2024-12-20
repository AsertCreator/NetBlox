﻿using NetBlox.Runtime;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NetBlox.Instances.Services
{
	[Service]
	public class SandboxService : Instance, I3DRenderable
	{
		[Lua([Security.Capability.CoreSecurity])]
		public bool Enabled { get; set; } = false;
		private bool firsttime = true;

		public SandboxService(GameManager ins) : base(ins) 
		{
		}

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(SandboxService) == classname) return true;
			return base.IsA(classname);
		}
		public void Render()
		{
			if (!Enabled) return;
			if (firsttime)
				GameManager.RenderManager.CurrentHint = "You're currently playing NetBlox Sandbox";
			firsttime = false;

			int mx = Raylib.GetMouseX();
			int my = Raylib.GetMouseY();
			var mp = Raylib.GetMouseRay(new System.Numerics.Vector2()
			{
				X = mx, Y = my
			}, 
			GameManager.RenderManager.MainCamera);
			var works = Root.GetService<Workspace>(true);

			for (int i = 0; i < GameManager.PhysicsManager.Actors.Count && false; i++)
			{
				var actor = GameManager.PhysicsManager.Actors[i];
				// TODO: this
			}

			if (works != null)
			{
				mp.Direction *= 30;

				var res = works.Raycast(new()
				{
					From = mp.Position,
					To = mp.Position + mp.Direction,
					MaxDistance = 500
				});

				if (res.Part != null && !res.Part.Locked)
				{
					Raylib.DrawCube(res.Part.Position, res.Part.Size.X, res.Part.Size.Y, res.Part.Size.Z, Color.Red);
					if (Raylib.IsMouseButtonPressed(MouseButton.Left))
					{
						RenderManager.LoadSound("rbxasset://sounds/boom.wav", GameManager.RenderManager.PlaySound);
						res.Part.Destroy();
					}
				}
			}
		}
	}
}
