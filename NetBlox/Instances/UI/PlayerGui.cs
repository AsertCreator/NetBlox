using NetBlox.Structs;
using Raylib_cs;
using Rectangle = NetBlox.Structs.Rectangle;

namespace NetBlox.Instances.UI;

public class PlayerGui : BasePlayerGui
{
    public override string ClassName => nameof(PlayerGui);

    public PlayerGui(ulong id, GameManager gameManager) : base(id, gameManager)
    {
        RegisterForEventId(GameEvent.EVENT_RENDERGUI_LEVEL0);
    }

    public override void OnRegisteredEvent(EngineEventArgs args)
    {
        base.OnRegisteredEvent(args);

        if (args.GameEvent.Id == GameEvent.EVENT_RENDERGUI_LEVEL0)
        {
            Render();
        }
    }

    public override bool IsA(string className)
    {
        if (className != nameof(PlayerGui))
            return base.IsA(className);
        return true;
    }
}