using System.Numerics;
using MoonSharp.Interpreter;
using NetBlox.Instances;
using NetBlox.Instances.Parts;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Runtime.Bridges;
using NetBlox.Structs;

namespace NetBlox.Network;

[Creatable]
[ExplorerIconIndex(12)]
public class Player : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public Model? Character
    { 
        get => GameManager.GameRegistry.GetLocalInstanceById(characterId) as Model;
        set => characterId = value != null ? value.InstanceID : 0;
    }
    public override Instance? Parent 
    {
        get => base.Parent;
        set
        {
            if (value == null)
            {
                Players? players = Parent as Players;
                if (players != null)
                    players.PlayerRemoving.Fire([DynValue.NewUserData(InstanceBridge.PushUserData(GameManager, this))]);
            }
            if (value != null)
            {
                Players? players = value as Players;
                if (players != null)
                    players.PlayerAdded.Fire([DynValue.NewUserData(InstanceBridge.PushUserData(GameManager, this))]);
            }
            base.Parent = value;
        }
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public string DisplayName { get; set; } = "";
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public long UserId => userId;

    public override string ClassName => nameof(Player);

    public Vector3 CurrentCameraPosition;
    public Vector3 CurrentCameraLookAt = new Vector3(0, 0, 1);

    public InstanceID characterId;
    public long userId;
    public bool hadInitialReplication;
    public CompoundConnection? Connection;

    public Player(ulong id, GameManager gameManager) : base(id, gameManager)
    {
        EnsureWritePlayer();
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.WritePlayer)]
    public void SetUserId(long userId)
    {
        EnsureWritePlayer();
        this.userId = userId;
    }
    public void EnsureWritePlayer()
    {
        SecurityIdentity? identity = GameManager.GameScheduler.GetCurrentSecurityIdentity();

        if (identity == null)
            throw new NetworkException("Cannot manipulate a Player instance without a secutiy identity");

        if (!identity.RequireSimpleCapability(SimpleSecurityCapabilityLevel.WritePlayer))
            throw new NetworkException("Cannot manipulate a Player instance without WritePlayer capability");
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public void Kick(string message)
    {
        if (GameManager.NetworkMode == NetworkMode.Client)
        {
            if (Root.GetService<Players>().LocalPlayer == this)
            {
                Root.GetService<NetworkClient>().InitiateUnilateralDisconnect(message);
            }
            else
            {
                throw new NetworkException("Cannot kick another player from a client");
            }
        }
        else
        {
            Root.GetService<NetworkServer>().ForceDisconnectPlayerWithMessage(this, message);
        }
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public BrickColor GetPlayerColor()
    {
        return BrickColor.AllBrickColors[Math.Abs(UserId) % BrickColor.AllBrickColors.Length];
    }

    public override void Destroy()
    {
        EnsureWritePlayer();
        base.Destroy();
    }

    public override bool IsA(string className)
    {
        if (className != nameof(Player))
            return base.IsA(className);
        return true;
    }
}