using NetBlox.Runtime;
using NetBlox.Instances;
using System.Numerics;
using Qu3e;
using Raylib_cs;

namespace NetBlox.Instances.Services
{
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
