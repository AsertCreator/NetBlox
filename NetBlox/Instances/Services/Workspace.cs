using NetBlox.Runtime;
using System.Numerics;
using Raylib_cs;

namespace NetBlox.Instances.Services
{
	public struct RaycastResult
	{
		public BasePart? Part;
		public float Distance;
		public Vector3 Normal;
		public Vector3 Where;
	}
	public struct RaycastRequest
	{
		public Vector3 From;
		public Vector3 To;
		public float MaxDistance;
		public BasePart? Ignore;
	}
	[Service]
	public class Workspace : Instance, I3DRenderable
	{
		[Lua([Security.Capability.None])]
		public Instance? MainCamera { get; set; }
		[Lua([Security.Capability.None])]
		public float Gravity { get; set; } = -9.8f;
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
		private bool birdAmbient = true;

		public Workspace(GameManager ins) : base(ins) 
		{ 
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
		public RaycastResult Raycast(RaycastRequest req)
		{
			return default;
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
		public void Render() { }
	}
}
