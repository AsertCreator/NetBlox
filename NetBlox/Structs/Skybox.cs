using Raylib_cs;

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

		public static Skybox LoadSkybox(string fp)
		{
			Skybox sb = new()
			{
				Back = Raylib.LoadTexture(GameManager.ContentFolder + $"skybox/{fp}_bk.png"),
				Bottom = Raylib.LoadTexture(GameManager.ContentFolder + $"skybox/{fp}_dn.png"),
				Front = Raylib.LoadTexture(GameManager.ContentFolder + $"skybox/{fp}_ft.png"),
				Left = Raylib.LoadTexture(GameManager.ContentFolder + $"skybox/{fp}_lf.png"),
				Right = Raylib.LoadTexture(GameManager.ContentFolder + $"skybox/{fp}_rt.png"),
				Top = Raylib.LoadTexture(GameManager.ContentFolder + $"skybox/{fp}_up.png")
			};

			return sb;
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