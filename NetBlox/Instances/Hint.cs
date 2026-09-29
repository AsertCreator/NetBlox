using System.Numerics;
using NetBlox.Rendering;
using NetBlox.Runtime;
using Raylib_cs;

namespace NetBlox.Instances;

[Creatable]
public class Hint : Message
{
    public override string ClassName => nameof(Hint);

    public Hint(ulong id, GameManager gameManager) : base(id, gameManager)
    {
    }

    public override void OnRegisteredEvent(EngineEventArgs args)
    {
        if (args.GameEvent.Id == GameEvent.EVENT_RENDERGUI_LEVEL2)
        {
            FontSpecification fontSpecification = GameManager.GameRenderer!.DefaultFontSpecification;
            Font font = GameManager.GameRenderer!.FontRegistry.LoadFontFromSpecification(fontSpecification);
            Vector2 vector2 = new Vector2(Raylib.GetScreenWidth() / 2, Raylib.GetScreenHeight() - fontSpecification.Size - 2);

            vector2.X -= Raylib.MeasureTextEx(font, Text, fontSpecification.Size, 0).X / 2;

            Raylib.DrawRectangle(0, 
                Raylib.GetScreenHeight() - (int)fontSpecification.Size - 4, 
                Raylib.GetScreenWidth(), (int)fontSpecification.Size + 4, Color.Black);
            Raylib.DrawTextEx(font, Text, vector2, fontSpecification.Size, 0, Color.White);
            
            return;
        }

        base.OnRegisteredEvent(args);
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Hint))
            return base.IsA(className);
        return true;
    }
}