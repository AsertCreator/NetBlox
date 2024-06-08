using Qu3e;
using Raylib_cs;
using System.Drawing;
using System.Numerics;
using static System.Formats.Asn1.AsnWriter;
using Transform = Qu3e.Transform;

namespace PhysicsTest
{
	internal class Program
	{
		internal static List<Actor> AllActors = [];
		internal static DateTime LastRenderTime = DateTime.Now;

		internal static void Main(string[] args)
		{
			BodyDef bd = new();
			Scene sc = new(1 / 60f, new Vector3(0, -9.8f, 0), 10);

			AddActor(new()
			{
				Color = Raylib_cs.Color.DarkGreen,
				Position = new Vector3(0, -15, 0),
				Size = new Vector3(2048, 4, 2048),
				Anchored = true
			}, sc);
			AddActor(new()
			{
				Position = new Vector3(0, 50, 0),
				Size = new Vector3(2, 50, 2)
			}, sc);

			RenderManager.Initialize(true);
			RenderManager.PostRender = () =>
			{
				sc.Step((DateTime.Now - LastRenderTime).TotalSeconds);
				LastRenderTime = DateTime.Now;
				for (int i = 0; i < AllActors.Count; i++)
				{
					var act = AllActors[i];
					act.Position = act.Body!.GetTransform().position;
					act.Rotation = act.Body!.GetTransform().rotation.ToEuler();
					act.Velocity = act.Body!.GetLinearVelocity();
				}
			};
			while (!Raylib.WindowShouldClose())
			{
				RenderManager.RenderFrame();
			}
		}
		internal static void AddActor(Actor act, Scene sc)
		{
			BodyDef bodyDef = new BodyDef();
			bodyDef.position.Set(act.Position.X, act.Position.Y, act.Position.Z);
			if (!act.Anchored)
				bodyDef.bodyType = BodyType.eDynamicBody;
			else
				bodyDef.bodyType = BodyType.eStaticBody;
			Body body = sc.CreateBody(bodyDef);
			BoxDef boxDef = new BoxDef();
			boxDef.Set(Transform.Identity, act.Size);
			act.Box = body.AddBox(boxDef);
			act.Body = body;

			AllActors.Add(act);
		}
	}
	internal class Actor
	{
		internal Vector3 Position;
		internal Vector3 Rotation;
		internal Vector3 Size = new(4, 1, 2);
		internal Raylib_cs.Color Color = Raylib_cs.Color.White;
		internal Vector3 Velocity;
		internal bool Anchored = false;
		internal bool CanCollide = true;
		internal Body? Body;
		internal Box? Box;
	}
}
