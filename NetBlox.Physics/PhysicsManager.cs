using MoonSharp.Interpreter;
using NetBlox.Instances;
using NetBlox.Instances.Services;
using Raylib_cs;
using System.Numerics;

namespace NetBlox
{
	public class PhysicsManager
	{
		public GameManager GameManager;
		public Workspace? Workspace => (Workspace?)GameManager.CurrentRoot.GetService<Workspace>(true);
		public float Gravity 
		{ 
			get => (Workspace ?? throw new ScriptRuntimeException("No workspace is loaded")).Gravity; 
			set => (Workspace ?? throw new ScriptRuntimeException("No workspace is loaded")).Gravity = value; 
		}
		public List<BasePart> Actors = new();
		public bool DisablePhysics = true; // not now

		public PhysicsManager(GameManager gameManager)
		{
			GameManager = gameManager;
		}
		public void ClientStep()
		{
			if (Workspace == null || DisablePhysics)
				return;

			var work = Workspace;
		}
		public void ServerStep()
		{
			if (Workspace == null || DisablePhysics)
				return;

			var work = Workspace;
		}
		public void Step()
		{
			try
			{
				if (GameManager.NetworkManager.IsServer)
					ServerStep();
				else if (GameManager.NetworkManager.IsClient)
					ClientStep();
			}
			catch (Exception e)
			{
				LogManager.LogError("Physics solver had failed! " + e.GetType() + ", msg:" + e.Message);
				LogManager.LogError(e.StackTrace ?? "no stacktrace");
				GameManager.Shutdown();
			}
		}
	}
}
