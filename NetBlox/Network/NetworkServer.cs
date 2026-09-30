using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using NetBlox.Instances;
using NetBlox.Instances.Parts;
using NetBlox.Instances.Services;
using NetBlox.Runtime;

namespace NetBlox.Network;

[Service]
[NotReplicated]
public class NetworkServer : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public int FirstMessageTimeout { get; set; }
    public override string ClassName => nameof(NetworkServer);

    public IPEndPoint? EndPoint => TcpListener == null ? null : TcpListener.LocalEndpoint as IPEndPoint;

    public const ulong NETWORK_CONSTANT_ID = 21;

    public ReplicationAgent ReplicationAgent;

    private TcpListener? TcpListener;
    private bool hadStarted = false;
    private List<BasePartPhysicsUpdateItem> updateItems = [];

    public record struct BasePartPhysicsUpdateItem(BasePart bp)
    {
        public BasePart Target = bp;
        public Vector3 NewPosition = bp.Position;
        public Vector3 NewOrientation = bp.Rotation;
        public Vector3 NewLinearVelocity = bp.Velocity;
        public Vector3 NewAngularVelocity = bp.AngularVelocity;
    }

    public NetworkServer(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
    {
        ReplicationAgent = new ReplicationAgent(gameManager);
    }

    public void ReportInvalidPacket(NetworkPacket packet, Player player)
    {
        ForceDisconnectPlayerWithMessage(player, "Invalid packet type " + packet.Type);
    }
    public void ReportVersionMismatch(NPServerboundHandshake.Entity packet, Player player)
    {
        if (packet.ProtocolVersion < NetworkPacketHandler.PROTOCOL_VERSION)
        {
            ForceDisconnectPlayerWithMessage(player, "Outdated client protocol version " +
                "(" + packet.ProtocolVersion + " < " + NetworkPacketHandler.PROTOCOL_VERSION + ")");
        }
        else
        {
            ForceDisconnectPlayerWithMessage(player, "Outdated server protocol version " +
                "(" + packet.ProtocolVersion + " > " + NetworkPacketHandler.PROTOCOL_VERSION + ")");
        }
    }

    public void AddBroadcastPhysicsUpdate(BasePart basePart)
    {
        BasePartPhysicsUpdateItem item = new BasePartPhysicsUpdateItem(basePart);
        updateItems.Add(item);
    }
    public void CommitBroadcastPhysicsUpdates()
    {
        NetworkPacket packet = NPPhysicsUpdate.Create(updateItems);
        updateItems.Clear();
        SendBroadcastNetworkPacketUnreliable(packet);
    }

    public void SendBroadcastNetworkPacketReliable(NetworkPacket networkPacket)
    {
        Players players = Root.GetService<Players>();
        Instance[] allPlayers = players.GetChildren();
        for (int i = 0; i < allPlayers.Length; i++)
        {
            Player? player = allPlayers[i] as Player;
            if (player == null)
                continue;
            
            player.Connection?.SendPacketReliable(networkPacket);
        }
    }
    public void SendBroadcastNetworkPacketUnreliable(NetworkPacket networkPacket)
    {
        Players players = Root.GetService<Players>();
        Instance[] allPlayers = players.GetChildren();
        for (int i = 0; i < allPlayers.Length; i++)
        {
            Player? player = allPlayers[i] as Player;
            if (player == null)
                continue;
            
            player.Connection?.SendPacketUnreliable(networkPacket);
        }
    }

    public void ForceDisconnectPlayerWithMessage(Player player, string message)
    {
        if (player.Connection == null)
            return;

        NetworkPacket networkPacket = NPKick.Create(message);
        player.Connection.SendPacketReliable(networkPacket);
        
        GameSchedulerTask gameSchedulerTask = GameManager.GameScheduler.Schedule("Connection Destroyer", GameScheduler.SchedulerPhase.Network, _ =>
        {
            Trace.TraceInformation("Kicking player " + player.Name + ": " + message);
            player.Connection.Disconnect();
            player.Connection = null;
            return SchedulerTaskResult.CompletedSuccess;
        });
        gameSchedulerTask.WaitingTimeTarget = GameManager.TimestampInTheFuture(TimeSpan.FromSeconds(1));
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public void StartServer(string endpointstring)
    {
        IPEndPoint iPEndPoint = IPEndPoint.Parse(endpointstring);
        Start(iPEndPoint);
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public void StopServer()
    {
        if (!hadStarted)
            throw new NetworkException("Cannot stop a stopped NetworkServer");

        hadStarted = false;
        
        Debug.Assert(TcpListener != null);

        Trace.TraceInformation("Stopping NetworkServer - disconnecting everyone");

        Instance[] instances = GetChildren();

        for (int i = 0; i < instances.Length; i++)
        {
            Player? player = instances[i] as Player;
            if (player == null)
                continue;
            player.Kick("Server is closing");
        }

        ClearAllChildren();

        Trace.TraceInformation("Stopping NetworkServer - stopping TcpListener");

        TcpListener.Stop();
        TcpListener.Dispose();
        TcpListener = null;

        GameManager.TryGetEventForId(GameEvent.EVENT_NETWORKSERVER_STOPPED)?.Fire();
    }

    public void Start(IPEndPoint at)
    {
        if (hadStarted)
            throw new NetworkException("Cannot start a NetworkServer a second time");
        
        hadStarted = true;
        TcpListener = new TcpListener(at);

        TcpListener.Start();
        TcpListener.BeginAcceptTcpClient(HandleNewTcpClient_Early, null);

        Trace.TraceInformation("Started NetworkServer, listening at port " + at.Port);

        GameManager.TryGetEventForId(GameEvent.EVENT_NETWORKSERVER_STARTED)?.Fire();

        GameSchedulerTask task = GameManager.GameScheduler.Schedule("Server Loop", GameScheduler.SchedulerPhase.Network, DoServerLoop);
        task.Identity = SecurityIdentity.SI_EngineNetworker;
    }

    private SchedulerTaskResult DoServerLoop(GameSchedulerTask task)
    {
        try
        {
            if (GameManager.ShuttingDown)
                return SchedulerTaskResult.CompletedSuccess;

            Players players = Root.GetService<Players>();
            using RentedSpan<Instance?> rented = players.GetChildren_Fast();

            bool hasAnyPlayers = false;

            for (int i = 0, j = 0; i < rented.Values.Length; i++)
            {
                Player? player = rented.Values[i] as Player;
                if (player == null)
                    continue;

                hasAnyPlayers = true;
            }

            if (!hasAnyPlayers)
            {
                ReplicationAgent.ForceFlushDeltaBuffer();
                return SchedulerTaskResult.NotCompleted;
            }

            byte[] frame = ReplicationAgent.CommitDeltaReplication();
            NPRespondDeltaReplication.Entity entity = default;

            entity.Flags = 0;
            entity.Payload = frame;

            NetworkPacket deltaPacket = NPRespondDeltaReplication.Create(entity);

            for (int i = 0, j = 0; i < rented.Values.Length; i++)
            {
                Player? player = rented.Values[i] as Player;
                if (player == null)
                    continue;

                if (player.hadInitialReplication)
                    player.Connection?.SendPacketReliable(deltaPacket);
            }        
            
            return SchedulerTaskResult.NotCompleted;
        }
        finally
        {
            int networkFps = 60;
            if (GameManager.GameRenderer != null)
                networkFps = GameManager.GameRenderer.PreferredFPS;
            task.WaitingTimeTarget = GameManager.TimestampInTheFuture(TimeSpan.FromMilliseconds(1000f / networkFps));
        }
    }

    private void HandleNewTcpClient_Early(IAsyncResult asyncResult)
    {
        if (!hadStarted || TcpListener == null)
        {
            Trace.TraceWarning("HandleNewTcpClient: TcpListener is null, not accepting a new client");
            return;
        }

        TcpClient tcpClient = TcpListener.EndAcceptTcpClient(asyncResult);
        TcpListener.BeginAcceptTcpClient(HandleNewTcpClient_Early, null);

        GameSchedulerTask task = GameManager.GameScheduler.ScheduleForImmediateExecution(
            "Handle New Client", GameScheduler.SchedulerPhase.Network, HandleNewTcpClient_Late);
        task.UserData = tcpClient;
        task.Identity = SecurityIdentity.SI_EngineNetworker;
    }
    private SchedulerTaskResult HandleNewTcpClient_Late(GameSchedulerTask gameSchedulerTask)
    {
        TcpClient? tcpClient = gameSchedulerTask.UserData as TcpClient;
        Debug.Assert(tcpClient != null);

        IPEndPoint? ipEndPoint = tcpClient.Client.RemoteEndPoint as IPEndPoint;
        Debug.Assert(ipEndPoint != null);

        IPEndPoint? localipEndPoint = tcpClient.Client.LocalEndPoint as IPEndPoint;
        Debug.Assert(localipEndPoint != null);

        UdpClient udpClient = new UdpClient(0, AddressFamily.InterNetwork);

        Player player = GameManager.GameRegistry.Construct<Player>();
        player.Connection = new CompoundConnection(tcpClient, udpClient);
        player.Connection.OnDisconnectedByOtherMeans += (_, _) =>
        {
            var task = GameManager.GameScheduler.ScheduleForImmediateExecution("Destroying a Disconnected Player Instance", GameScheduler.SchedulerPhase.Network, _ =>
            {
                Trace.TraceInformation("Disconnecting player " + player.Name + ": broken connection");
                player.Destroy();
                return SchedulerTaskResult.CompletedSuccess; 
            }); 
            task.Identity = SecurityIdentity.SI_EngineNetworker;
        };
        player.Connection.OnFirstMessageTimeout += (_, _) =>
        {
            var task = GameManager.GameScheduler.ScheduleForImmediateExecution("Destroying a Timeouted Player Instance", GameScheduler.SchedulerPhase.Network, _ =>
            {
                Trace.TraceInformation("Disconnecting player " + player.Name + ": timeout");
                player.Connection.Dispose();
                player.Destroy();
                return SchedulerTaskResult.CompletedSuccess; 
            }); 
            task.Identity = SecurityIdentity.SI_EngineNetworker;
        };

        // for performance received packets are not handled on the main thread, instead networkpackethandler's should create
        // their scheduler tasks when needed, for example to change the datamodel hierarchy.

        player.Connection.OnMessage += (_, message) =>
        {
            NetworkPacketHandler? packetHandler =  NetworkPacketHandler.ForPacketType(message.Type);
            if (packetHandler == null)
            {
                player.Kick("Unknown packet type = " + message.Type);
                return;
            }

            packetHandler.HandleServerboundPacket(message, GameManager, player);
        };

        player.Connection.StartReading();

        player.Connection.ResetFirstMessageTimeoutTimer(FirstMessageTimeout);

        NPUnreliableConnectionInfo.Entity entity = default;
        IPEndPoint? iPEndPoint = player.Connection.UdpClient!.Client.LocalEndPoint as IPEndPoint;

        Debug.Assert(iPEndPoint != null);

        entity.UdpPort = (ushort)iPEndPoint.Port;

        player.Connection.SendPacketReliable(NPUnreliableConnectionInfo.Create(entity));

        return SchedulerTaskResult.CompletedSuccess;
    }

    public override void Destroy()
    {
        if (!GameManager.GameScheduler.GetCurrentSecurityIdentity()!.RequireSimpleCapability(SimpleSecurityCapabilityLevel.DestroyServices))
            return;
        if (hadStarted)
            StopServer();
        base.Destroy();
    }

    public override bool IsA(string className)
    {
        if (className != nameof(NetworkServer))
            return base.IsA(className);
        return true;
    }
}