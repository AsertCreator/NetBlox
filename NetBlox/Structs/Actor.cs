using NetBlox.Instances;
using NetBlox.Instances.Services;
using Qu3e;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace NetBlox.Structs
{
	public class Actor
	{
		public Vector3 Position;
		public Vector3 Rotation;
		public Vector3 Size;
		public Vector3 Velocity;
		public bool Anchored = false;
		public bool CanCollide = true;
		public BasePart BasePart;
		public Body Body;
		public Box Box;

		public Actor(BasePart bp)
		{
			BasePart = bp;
			Position = bp.Position;
			Rotation = bp.Rotation;
			Size = bp.Size;
			Velocity = bp.Velocity;
			Anchored = bp.Anchored;
			CanCollide = bp.CanCollide;

			Scene sc = BasePart.GameManager.CurrentRoot.GetService<Workspace>().Scene;
			lock (sc)
			{
				BodyDef bodyDef = new BodyDef();
				bodyDef.position.Set(Position.X, Position.Y, Position.Z);
				if (!Anchored)
					bodyDef.bodyType = BodyType.eDynamicBody;
				else
					bodyDef.bodyType = BodyType.eStaticBody;
				Body body = sc.CreateBody(bodyDef);
				BoxDef boxDef = new BoxDef();
				boxDef.Set(Transform.Identity, Size);
				Box = body.AddBox(boxDef);
				Body = body;
			}
		}
		public void Update()
		{
			CanCollide = BasePart.CanCollide;
			Anchored = BasePart.Anchored;

			if (!Anchored)
				Body.Flags &= ~BodyFlags.eStatic;
			else
				Body.Flags &= ~BodyFlags.eDynamic;

			BasePart.Position = Position;
			BasePart.Rotation = Rotation;
			BasePart.Velocity = Velocity;
		}
		public void Remove()
		{
			Scene sc = BasePart.GameManager.CurrentRoot.GetService<Workspace>().Scene;
			lock (sc)
			{
				sc.RemoveBody(Body);
				Body = null!;
				Box = null!;
			}
		}
	}
}
