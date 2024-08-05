using NetBlox.Runtime;
using NetBlox.Instances;
using System.Numerics;
using Qu3e;
using Raylib_cs;

namespace NetBlox.Instances.Services
{
	public struct RaycastResult
	{
		public BasePart? Part;
		public float Distance;
	}
	public struct RaycastRequest
	{
		public Vector3 From;
		public Vector3 To;
		public float MaxDistance;
	}
	public class Workspace : Instance, I3DRenderable
	{
		[Lua([Security.Capability.None])]
		public Instance? MainCamera { get; set; }
		[Lua([Security.Capability.None])]
		public Vector3 Gravity { get; set; } = new Vector3(0, -9.8f, 0);
		[Lua([Security.Capability.None])]
		public float FallenPartsDestroyHeight { get; set; } = -50;
		[Lua([Security.Capability.None])]
		public bool BirdAmbient 
		{ 
			get => birdAmbient; 
			set 
			{
				if (Ambient.HasValue && GameManager.NetworkManager.IsClient && birdAmbient && !value)
					GameManager.RenderManager.StopSound(Ambient.Value);
				birdAmbient = value;
			} 
		}
		public SpawnLocation? SpawnLocation;
		public Sound? Ambient;
		public Scene Scene;
		private bool birdAmbient = true;

		public Workspace(GameManager ins) : base(ins) 
		{ 
			Scene = new(1 / AppManager.PreferredFPS, Gravity, 10);
			birdAmbient = true;
			if (GameManager.NetworkManager.IsClient)
			{
				RenderManager.LoadSound("rbxasset://sounds/birdsambient.mp3", x => Ambient = x);
			}
		}

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Workspace) == classname) return true;
			return base.IsA(classname);
		}
		[Lua([Security.Capability.None])]
		public void ZoomToExtents()
		{
			GameManager.RenderManager.MainCamera.Position = new Vector3(50, 40, 0);
			GameManager.RenderManager.MainCamera.Target = Vector3.Zero;
		}
		private class RaycastQueryCallback(WeakReference reference) : QueryCallback
		{
			public WeakReference Reference = reference;
			public override bool ReportShape(Box box)
			{
				Reference.Target = box;
				return true;
			}
		}
		public RaycastResult Raycast(RaycastRequest req)
		{
			// Qu3e has the best api naming i ever seen
			// we need to resynchronize proxies. whatever that means
			for (int i = 0; i < GameManager.PhysicsManager.Actors.Count; i++)
			{
				var actor = GameManager.PhysicsManager.Actors[i];
				actor.Body.SynchronizeProxies();
			}

			WeakReference boxref = new WeakReference(null);
			Scene.RayCast(new RaycastQueryCallback(boxref /*im desperate*/), new RaycastData()
			{
				start = req.From,
				dir = Vector3.Normalize(req.To - req.From),
				t = req.MaxDistance
			});
			if (boxref.Target == null)
			{
				return new RaycastResult()
				{
					Distance = -1,
					Part = null
				};
			}
			var basepart = ((Box)(boxref.Target)).GetUserdata() as BasePart; // was about to go with O(n^2) approach.
			// is performance is really a concern in THIS project? in a roblox clone?? should i even care? should you?
			if (basepart != null && !basepart.Locked)
			{
				return new RaycastResult()
				{
					Distance = Vector3.Distance(basepart.Position, req.To),
					Part = basepart
				};
			}

			return new RaycastResult()
			{
				Distance = -1,
				Part = null
			};
		}
		public override void Process()
		{
			base.Process();
			GameManager.PhysicsManager.Step();
			if (GameManager.NetworkManager.IsClient && BirdAmbient && Ambient.HasValue) 
			{
				if (!GameManager.RenderManager.IsSoundPlaying(Ambient.Value))
					GameManager.RenderManager.PlaySound(Ambient.Value);
			}
		}
		public void Render()
		{
			if (GameManager.NetworkManager.IsServer)
			{
				int halfslices = 50 / 2; 
				Rlgl.Begin(1);

				for (int i = -halfslices; i <= halfslices; i++)
				{
					if (i == 0)
					{
						Rlgl.Color3f(0.5f, 0.5f, 0.5f);
						Rlgl.Color3f(0.5f, 0.5f, 0.5f);
						Rlgl.Color3f(0.5f, 0.5f, 0.5f);
						Rlgl.Color3f(0.5f, 0.5f, 0.5f);
					}
					else
					{
						Rlgl.Color3f(0.75f, 0.75f, 0.75f);
						Rlgl.Color3f(0.75f, 0.75f, 0.75f);
						Rlgl.Color3f(0.75f, 0.75f, 0.75f);
						Rlgl.Color3f(0.75f, 0.75f, 0.75f);
					}

					Rlgl.Vertex3f((float)i * 3, FallenPartsDestroyHeight, (float)-halfslices * 3);
					Rlgl.Vertex3f((float)i * 3, FallenPartsDestroyHeight, (float)halfslices * 3);

					Rlgl.Vertex3f((float)-halfslices * 3, FallenPartsDestroyHeight, (float)i * 3);
					Rlgl.Vertex3f((float)halfslices * 3, FallenPartsDestroyHeight, (float)i * 3);
				}

				Rlgl.End();

				RenderUtils.DrawCubeFaced(new Vector3(0, FallenPartsDestroyHeight, 0), Vector3.Zero, 
					halfslices * 2 * 3, 0, halfslices * 2 * 3, new Color(255, 50, 50, 50), Faces.Top | Faces.Bottom);
			}
		}
	}
}
