using System.Numerics;
using NetBlox.Rendering;
using NetBlox.Runtime;
using Raylib_cs;

namespace NetBlox.Instances;

[Creatable]
public class Message : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public string Text { get; set; } = "";
    public override string ClassName => nameof(Message);

    public Message(ulong id, GameManager gameManager) : base(id, gameManager)
    {
        RegisterForEventId(GameEvent.EVENT_RENDERGUI_LEVEL2);
    }

    public override void OnRegisteredEvent(EngineEventArgs args)
    {
        base.OnRegisteredEvent(args);

        if (args.GameEvent.Id == GameEvent.EVENT_RENDERGUI_LEVEL2)
        {
            FontSpecification fontSpecification = GameManager.GameRenderer!.DefaultFontSpecification;
            fontSpecification.Size += 4;
            Font font = GameManager.GameRenderer!.FontRegistry.LoadFontFromSpecification(fontSpecification);
            Vector2 vector2 = new Vector2(Raylib.GetScreenWidth() / 2, Raylib.GetScreenHeight() / 2);
            vector2 -= Raylib.MeasureTextEx(font, Text, fontSpecification.Size, 0) / 2;
            Raylib.DrawRectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), new Color(80, 80, 80, 127));

            // this could be made better with a shader but im not doing that

            Raylib.DrawTextEx(font, Text, new Vector2(vector2.X + 1, vector2.Y + 1), fontSpecification.Size, 0, Color.Black);
            Raylib.DrawTextEx(font, Text, new Vector2(vector2.X + 1, vector2.Y + 0), fontSpecification.Size, 0, Color.Black);
            Raylib.DrawTextEx(font, Text, new Vector2(vector2.X + 1, vector2.Y - 1), fontSpecification.Size, 0, Color.Black);
            Raylib.DrawTextEx(font, Text, new Vector2(vector2.X + 0, vector2.Y + 1), fontSpecification.Size, 0, Color.Black);
            Raylib.DrawTextEx(font, Text, new Vector2(vector2.X + 0, vector2.Y - 1), fontSpecification.Size, 0, Color.Black);
            Raylib.DrawTextEx(font, Text, new Vector2(vector2.X - 1, vector2.Y + 1), fontSpecification.Size, 0, Color.Black);
            Raylib.DrawTextEx(font, Text, new Vector2(vector2.X - 1, vector2.Y + 0), fontSpecification.Size, 0, Color.Black);
            Raylib.DrawTextEx(font, Text, new Vector2(vector2.X - 1, vector2.Y - 1), fontSpecification.Size, 0, Color.Black);

            Raylib.DrawTextEx(font, Text, vector2, fontSpecification.Size, 0, Color.White);
        }
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Message))
            return base.IsA(className);
        return true;
    }
}