using NetBlox.Instances.Services;
using NetBlox.Structs;
using Qu3e;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace NetBlox
{
	public class PhysicsManager
	{
		public GameManager GameManager;
		public Workspace? Workspace => (Workspace?)GameManager.CurrentRoot.FindFirstChild("Workspace");
		public Vector3 Gravity { get => (Workspace ?? throw new Exception("No workspace is loaded")).Gravity; set => (Workspace ?? throw new Exception("No workspace is loaded")).Gravity = value; }
		public Scene Scene { get => (Workspace ?? throw new Exception("No workspace is loaded")).Scene; set => (Workspace ?? throw new Exception("No workspace is loaded")).Scene = value; }
		public List<Actor> Actors = new();
		private DateTime LastTime;

		public PhysicsManager(GameManager gameManager)
		{
			GameManager = gameManager;
		}
		public void Begin() => LastTime = DateTime.Now;
		public void Step()
		{
			if (Workspace == null || Scene == null)
				return;
			lock (Scene)
			{
				Scene.Step((DateTime.Now - LastTime).TotalSeconds);
				for (int i = 0; i < Actors.Count; i++)
				{
					var act = Actors[i];
					act.Position = act.Body!.GetTransform().position;
					act.Rotation = act.Body!.GetTransform().rotation.ToEuler();
					act.Velocity = act.Body!.GetLinearVelocity();
					act.Update();
				}
			}
		}
	}
}
