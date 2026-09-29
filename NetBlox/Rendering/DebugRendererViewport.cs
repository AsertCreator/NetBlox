using System.Numerics;
using NetBlox.Structs;
using Raylib_cs;

namespace NetBlox.Rendering;

public class DebugRendererViewport : RendererViewport
{
    public DebugRendererViewport(GameRenderer gameRenderer) : base(gameRenderer)
    {
    }

    public override void RenderFrame()
    {
        Raylib.ClearBackground(Color.LightGray);

        Raylib.DrawText("BrickColors:", 20, 20, 15, Color.Black);

        BrickColor[] brickColors = BrickColor.AllBrickColors;

        for (int i = 0; i < brickColors.Length; i++)
        {
            BrickColor brickColor = brickColors[i];

            Raylib.DrawRectangle(20 + i * 6, 40, 6, 200, brickColor.Color3);

            Raylib.DrawTextPro(Raylib.GetFontDefault(), brickColor.Index.ToString(), 
                new Vector2(20 + i * 6, 255), new Vector2(0, 0), -90, 7, 0.6f, Color.Black);
        }
    }
}