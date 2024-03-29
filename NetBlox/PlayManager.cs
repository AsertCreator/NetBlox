using NetBlox.Instances.Services;
using NetBlox.Instances;
using NetBlox.Runtime;
using NetBlox.GUI;
using NetBlox.Structs;
using NetBlox.Tools;

namespace NetBlox
{
	public static class PlayManager
	{
		public static string ContentFolder = "content/";
		public static GUI.GUI? CurrentTeleportGUI;
		public static Dictionary<char, Action> Verbs = new();

		public static void Initialize()
		{
			// no verbs as of now
		}
		public static void ShowTeleportGui()
		{
			var guitext = new GUIText("Loading place...", new UDim2(0.5f, 0.5f))
			{
				Color = Color.White,
				FontSize = 24
			};
			var guiframe = new GUIFrame(new UDim2(1, 1), new UDim2(0.5f, 0.5f), Color.DarkBlue);

			CurrentTeleportGUI = new GUI.GUI()
			{
				CorrespondingPhase = GameplayPhase.Loading,
				Elements = new()
				{
					guiframe,
					guitext
				}
			};

			RenderManager.ScreenGUI.Add(CurrentTeleportGUI);
		}
		public static void HideTeleportGui()
		{
			var fc = RenderManager.Framecount;

			RenderManager.Coroutines.Add(() =>
			{
				(CurrentTeleportGUI!.Elements[0] as GUIFrame)!.Color.A -= 255 / 20; // very very very fucky hacky

				if (RenderManager.Framecount - fc == 20)
				{
					GameManager.CurrentGameplayPhase = GameplayPhase.Gameplay;
					RenderManager.ScreenGUI.Remove(CurrentTeleportGUI);
					CurrentTeleportGUI = null;
					return -1;
				}

				return 0;
			});
		}
		public static void ShowKickMessage(string msg)
		{
			RenderManager.ScreenGUI.Add(new GUI.GUI()
			{
				CorrespondingPhase = GameManager.CurrentGameplayPhase,
				Elements = {
					new GUIFrame(new UDim2(0.25f, 0.175f), new UDim2(0.5f, 0.5f), Color.Red),
					new GUIText("You've been kicked from this server: " + msg + ".\nYou may or may not been banned from this place.", new UDim2(0.5f, 0.5f))
				}
			});
		}
	}
}
