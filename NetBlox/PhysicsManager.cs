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
		public float Gravity { get => (Workspace ?? throw new Exception("No workspace is loaded")).Gravity; set => (Workspace ?? throw new Exception("No workspace is loaded")).Gravity = value; }
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
					Scene.Step((DateTime.UtcNow - LastTime).TotalSeconds * 2.5);

					for (int i = 0; i < Actors.Count; i++)
					{
						var act = Actors[i];
						if (!act.Anchored && ((GameManager.NetworkManager.IsClient && GameManager.SelfOwnerships.Contains(act)) || 
							(GameManager.NetworkManager.IsServer && !GameManager.Owners.ContainsValue(act))))
						{
							act._position = act.Body!.GetTransform().position;
							act._rotation = act.Body!.GetTransform().rotation.ToEuler();
							act._lastvelocity = act.Body!.GetLinearVelocity();

							if (act._position.Y < Workspace.FallenPartsDestroyHeight && GameManager.NetworkManager.IsServer)
							{
								act.Destroy();
								continue;
							}

							if ((GameManager.NetworkManager.Clients.Count > 0 || GameManager.NetworkManager.IsClient) && (
								act._lastposition != act._position ||
								act._lastrotation != act._rotation ||
								act._lastvelocity != act.Velocity))
								act.ReplicateProperties(["Position", "Rotation", "Velocity"], false);

							act._lastposition = act._position;
							act._lastrotation = act._rotation;
						}
						else
						{
							act.Body!.SetTransform(act._position, act._rotation);
							act.Body!.SetLinearVelocity(act._lastvelocity);
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
