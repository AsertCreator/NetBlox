using NetBlox.Instances;
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
		public Workspace? Workspace => (Workspace?)GameManager.CurrentRoot.GetService<Workspace>(true);
		public Vector3 Gravity { get => (Workspace ?? throw new Exception("No workspace is loaded")).Gravity; set => (Workspace ?? throw new Exception("No workspace is loaded")).Gravity = value; }
		public Scene Scene { get => (Workspace ?? throw new Exception("No workspace is loaded")).Scene; set => (Workspace ?? throw new Exception("No workspace is loaded")).Scene = value; }
		public List<BasePart> Actors = new();
		public bool DisablePhysics = true; // not now
		private DateTime LastTime = DateTime.UtcNow;

		public PhysicsManager(GameManager gameManager)
		{
			GameManager = gameManager;
		}
		public void Begin() => LastTime = DateTime.UtcNow;
		public void Step()
		{
			if (Workspace == null || Scene == null || DisablePhysics)
				return;
			lock (Scene)
			{
				try
				{
					Scene.Step((DateTime.UtcNow - LastTime).TotalSeconds);
					for (int i = 0; i < Actors.Count; i++)
					{
						var act = Actors[i];
						if (!act.Anchored)
						{
							act._position = act.Body!.GetTransform().position;
							act._rotation = act.Body!.GetTransform().rotation.ToEuler();
							act.Velocity = act.Body!.GetLinearVelocity();

							if (act._position.Y < Workspace.FallenPartsDestroyHeight && GameManager.NetworkManager.IsServer)
							{
								act.Destroy();
								continue;
							}

							if (GameManager.NetworkManager.Clients.Count > 0 ||
								act._lastposition != act._position ||
								act._lastrotation != act._rotation ||
								act._lastvelocity != act.Velocity)

								act.ReplicateProperties(["Position", "Rotation"], false);

							act._lastposition = act._position;
							act._lastrotation = act._rotation;
							act._lastvelocity = act.Velocity;
						}
					}
					LastTime = DateTime.UtcNow;
				}
				catch (Exception ex)
				{
					LogManager.LogWarn("Physics solver had failed! " + ex.GetType() + ", msg: " + ex.Message);
				}
			}
		}
	}
}
