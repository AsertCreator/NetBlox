using System.Diagnostics;
using System.Net;
using NetBlox.Instances;
using NetBlox.Runtime;

namespace NetBlox.Network;

[Service]
[NotReplicated]
public class NetworkClient : Instance
{
    public CompoundConnection? CurrentServerConnection;
    public string? CurrentUsername;
    public string? CurrentServerVersionString;

    public bool IsConnected => CurrentServerConnection == null ? false : CurrentServerConnection.IsConnected;

    public override string ClassName => nameof(NetworkClient);

    public new ReplicationAgent ReplicationAgent;

    public TimeSpan LastServerPingValue;

    private bool waitingForPing;
    private GlobalTimestamp waitingForPingLastTime;

    public const ulong NETWORK_CONSTANT_ID = 20;

    public NetworkClient(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
    {
        ReplicationAgent = new ReplicationAgent(gameManager);
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public void ConnectToServer(string endpointstring, int firstMessageTimeout)
    {
        try
        {
            if (IsConnected)
                throw new InvalidOperationException("Cannot connect to a server when already connected");

            IPEndPoint iPEndPoint = IPEndPoint.Parse(endpointstring);
            CurrentServerConnection = new CompoundConnection();
            CurrentServerConnection.Connect(iPEndPoint, firstMessageTimeout);

            CurrentServerConnection.OnDisconnectedByOtherMeans += (_, _) =>
            {
                GameManager.GameScheduler.ScheduleForImmediateExecution("Disconnecting From Server", GameScheduler.SchedulerPhase.Network, _ =>
                {
                    InitiateUnilateralDisconnect("Server connection failure; probably server has crashed");
                    return SchedulerTaskResult.CompletedSuccess; 
                }); 
            };
            CurrentServerConnection.OnFirstMessageTimeout += (_, _) =>
            {
                GameManager.GameScheduler.ScheduleForImmediateExecution("Disconnecting From Timed Out Server", GameScheduler.SchedulerPhase.Network, _ =>
                {
                    InitiateUnilateralDisconnect("Connection timeout");
                    return SchedulerTaskResult.CompletedSuccess; 
                }); 
            };

            // for performance received packets are not handled on the main thread, instead networkpackethandler's should create
            // their scheduler tasks when needed, for example to change the datamodel hierarchy.

            CurrentServerConnection.OnMessage += (_, message) =>
            {
                NetworkPacketHandler? packetHandler =  NetworkPacketHandler.ForPacketType(message.Type);
                if (packetHandler == null)
                {
                    InitiateUnilateralDisconnect("Unknown packet type = " + message.Type);
                    return;
                }

                packetHandler.HandleClientboundPacket(message, GameManager, CurrentServerConnection);
            };

            if (!CurrentServerConnection.IsConnected)
                throw new NetworkException("Failed to connect; no further details");

            GameManager.GameScheduler.Schedule("Ping Measurement", GameScheduler.SchedulerPhase.Network, _ =>
            {
                if (CurrentServerConnection != null && CurrentServerConnection.IsConnected)
                {
                    if (!waitingForPing)
                    {
                        NetworkPacket networkPacket = NPPing.Create(Random.Shared.Next());
                        waitingForPingLastTime = GameManager.CurrentGlobalTimestamp();
                        CurrentServerConnection.SendPacketReliable(networkPacket);
                    }
                    else
                    {
                        TimeSpan timeSpan = TimeSpan.FromMicroseconds(GameManager.CurrentGlobalTimestamp() - waitingForPingLastTime);
                        if (timeSpan.TotalSeconds > 20)
                        {
                            InitiateUnilateralDisconnect("Connection timeout");
                            waitingForPing = false;
                        }
                    }

                    _.WaitingTimeTarget = GameManager.TimestampInTheFuture(TimeSpan.FromSeconds(1));
                    return SchedulerTaskResult.NotCompleted;
                }
                return SchedulerTaskResult.CompletedSuccess;
            });

            GameManager.TryGetEventForId(GameEvent.EVENT_NETWORKCLIENT_STARTED)?.Fire();
        }
        catch (Exception ex)
        {
            throw new NetworkException("Failed to connect; " + ex.Message);
        }
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public void InitiateUnilateralDisconnect(string message)
    {
        if (CurrentServerConnection == null)
            return;

        Trace.TraceInformation("InitiateUnilateralDisconnect: " + message);

        CurrentServerConnection.Disconnect();
        CurrentServerConnection = null;

        if (GameManager.GameRenderer != null)
        {
            GameManager.GameRenderer.StatusText = "Disconnected from server: " + message;
        }

        GameManager.TryGetEventForId(GameEvent.EVENT_NETWORKCLIENT_STOPPED)?.Fire();
        GameManager.TryGetEventForId(GameEvent.EVENT_KICKED)?.Fire(message);

        Root.RunService?.Stop();
    }
    public void ReportInvalidPacket(NetworkPacket networkPacket)
    {
        string errorMessage = "Invalid packet received - " + networkPacket.Type + ", cannot handle it";
        Trace.TraceError(errorMessage);

        InitiateUnilateralDisconnect(errorMessage);
    }
    public void ReportPing()
    {
        ulong microseconds = GameManager.CurrentGlobalTimestamp() - waitingForPingLastTime;
        LastServerPingValue = TimeSpan.FromMicroseconds(microseconds);
        waitingForPing = false;
    }

    public override void Destroy()
    {
        if (!GameManager.GameScheduler.GetCurrentSecurityIdentity()!.RequireSimpleCapability(SimpleSecurityCapabilityLevel.DestroyServices))
            return;
        if (IsConnected)
            InitiateUnilateralDisconnect("Client is closing");
        base.Destroy();
    }

    public override bool IsA(string className)
    {
        if (className != nameof(NetworkClient))
            return base.IsA(className);
        return true;
    }
}