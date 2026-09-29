using Raylib_cs;

namespace NetBlox.Rendering;

public class FontRegistry
{
    public GameRenderer GameRenderer;
    private Dictionary<FontSpecification, Font> loadedCache = new Dictionary<FontSpecification, Font>();

    public FontRegistry(GameRenderer gameRenderer)
    {
        GameRenderer = gameRenderer;
    }

    public Font LoadFontFromSpecification(FontSpecification fontSpecification)
    {
        if (loadedCache.TryGetValue(fontSpecification, out Font value))
            return value;

        Font font = Raylib.LoadFontEx(fontSpecification.FontFilePath, (int)fontSpecification.Size * GameRenderer.DpiAwareCellSize, null, 0);
        loadedCache[fontSpecification] = font;
        return font;
    }
    public void UnloadFontFromSpecification(FontSpecification fontSpecification)
    {
        if (!loadedCache.ContainsKey(fontSpecification))
            return;
        Font font = loadedCache[fontSpecification];
        Raylib.UnloadFont(font);
        loadedCache.Remove(fontSpecification);
    }
    public void UnloadAllFonts()
    {
        foreach (KeyValuePair<FontSpecification, Font> fontPair in loadedCache)
        {
            Font font = fontPair.Value;
            Raylib.UnloadFont(font);
        }
        loadedCache.Clear();
    }
    public FontSpecification[] AllCachedFontSpecifications() => loadedCache.Keys.ToArray();
}