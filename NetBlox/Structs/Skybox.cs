using Raylib_cs;
using System.Resources;

namespace NetBlox.Structs
{
	public class Skybox
	{
		public bool SkyboxWires = false;
		public bool SkyboxMoves = true;
		public int SkyboxSize = 999;
		public Texture2D Top;
		public Texture2D Bottom;
		public Texture2D Left;
		public Texture2D Right;
		public Texture2D Front;
		public Texture2D Back;

		private Skybox() { }

		public static Skybox LoadSkybox(GameManager gm, string fp)
		{
			if (gm.RenderManager == null) return new();

			Skybox sk = new();

			RenderManager.LoadTexture($"rbxasset://skybox/{fp}_bk.png", x => sk.Back = x);
			RenderManager.LoadTexture($"rbxasset://skybox/{fp}_dn.png", x => sk.Bottom = x);
			RenderManager.LoadTexture($"rbxasset://skybox/{fp}_ft.png", x => sk.Front = x);
			RenderManager.LoadTexture($"rbxasset://skybox/{fp}_lf.png", x => sk.Left = x);
			RenderManager.LoadTexture($"rbxasset://skybox/{fp}_rt.png", x => sk.Right = x);
			RenderManager.LoadTexture($"rbxasset://skybox/{fp}_up.png", x => sk.Top = x);

			return sk;
		}
		public void Unload()
		{
			Raylib.UnloadTexture(Front);
			Raylib.UnloadTexture(Top);
			Raylib.UnloadTexture(Left);
			Raylib.UnloadTexture(Right);
			Raylib.UnloadTexture(Bottom);
			Raylib.UnloadTexture(Back);
		}
	}
}
