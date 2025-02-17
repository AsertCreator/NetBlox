using NetBlox.Instances.Services;
using NetBlox.Runtime;
using Raylib_cs;
using System.Numerics;

namespace NetBlox.Instances
{
	public enum HopperType
	{
		Script, Drag, Clone, Destroy
	}
	[Creatable]
	public class Hopper : Tool
	{
		[Lua([Security.Capability.None])]
		public HopperType BinType { get; set; }

		public Hopper(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Hopper) == classname) return true;
			return base.IsA(classname);
		}
		[Lua([Security.Capability.None])]
		public override string GetIcon()
		{
			switch (BinType)
			{
				case HopperType.Drag:
					return "rbxasset://hopperDrag.png";
				case HopperType.Clone:
					return "rbxasset://hopperClone.png";
				case HopperType.Destroy:
					return "rbxasset://hopperDestroy.png";
				default:
					return "rbxasset://blank.png";
			}
		}
		[Lua([Security.Capability.CoreSecurity])]
		public override void Activate() 
		{
			switch (BinType)
			{
				case HopperType.Drag:
					StartDragging();
					break;
			}
		}
		[Lua([Security.Capability.CoreSecurity])]
		public override void SetSelected() { }
		[Lua([Security.Capability.CoreSecurity])]
		public override void SetUnselected() { }

		public BasePart? CapturePart()
		{
			int mx = Raylib.GetMouseX();
			int my = Raylib.GetMouseY();
			var mp = Raylib.GetScreenToWorldRay(new Vector2()
			{
				X = mx,
				Y = my
			},
			GameManager.RenderManager.MainCamera);
			var works = Root.GetService<Workspace>(true);

			if (works != null)
			{
				var res = works.Raycast(new()
				{
					From = mp.Position,
					To = mp.Position + mp.Direction,
					MaxDistance = 500
				});

				if (res.Part != null && !res.Part.Locked)
					return res.Part;
			}

			return null;
		}
		public (Vector3, Vector3)? GetMouseV3(BasePart? except)
		{
			int mx = Raylib.GetMouseX();
			int my = Raylib.GetMouseY();
			var mp = Raylib.GetScreenToWorldRay(new Vector2()
			{
				X = mx,
				Y = my
			},
			GameManager.RenderManager.MainCamera);
			var works = Root.GetService<Workspace>(true);

			if (works != null)
			{
				var res = works.Raycast(new()
				{
					From = mp.Position,
					To = mp.Position + mp.Direction,
					MaxDistance = 500,
					Ignore = except
				});

				if (res.Distance != -1)
					return (res.Where, res.Normal);
				return null;
			}

			return null;
		}
		public void StartDragging()
		{
			var part = CapturePart();
			var selection = new SelectionBox(GameManager);

			selection.Adornee = part;
			selection.Parent = part;

			if (part == null)
				return;

			var works = Root.GetService<Workspace>(true);

			if (works != null)
			{
				Task.Run(async () =>
				{
					var og = part.Position;

					while (Raylib.IsMouseButtonDown(MouseButton.Left))
					{
						var v3 = GetMouseV3(part);

						if (!v3.HasValue)
						{
							part.Position = og;
							continue;
						}

						var pos = v3.Value.Item1;
						var normal = v3.Value.Item2;

						part.Position = pos + (normal * part.Size);

						await Task.Delay(1000 / Raylib.GetFPS());
					}

					selection.Destroy();
				});
			}
		}
	}
}
