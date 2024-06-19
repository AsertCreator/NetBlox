using Raylib_CsLo;

namespace NetBlox
{
	public static class ResourceManager
	{
		public static Dictionary<string, Font> FontCache = [];
		public static Dictionary<string, Texture> TextureCache = [];

		public static Font GetFont(string name)
		{
			if (FontCache.ContainsKey(name)) return FontCache[name];
			FontCache[name] = Raylib.LoadFont(name);
			return FontCache[name];
		}
		public static Texture GetTexture(string name)
		{
			if (TextureCache.ContainsKey(name)) return TextureCache[name];
			TextureCache[name] = Raylib.LoadTexture(name);
			return TextureCache[name];
		}
	}
}
