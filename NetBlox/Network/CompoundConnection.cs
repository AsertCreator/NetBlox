using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace NetBlox.Network;

public delegate void NetworkMessageEventHandler(object? sender, NetworkPacket packet);

public class CompoundConnection : IDisposable
{
    public IPEndPoint? EndPoint => TcpClient.Client.RemoteEndPoint as IPEndPoint;
    public bool IsConnected => connected && TcpClient.Connected;

    private Timer firstMessageTimoutTimer;
    public TcpClient TcpClient;
    public UdpClient? UdpClient;
    public IPEndPoint? UdpClientEndpoint;

    private bool connected;
    private bool disposed;
    private bool everReceivedMessages;

    public event EventHandler? OnDisconnectedByOtherMeans;
    public event EventHandler? OnFirstMessageTimeout;
    public event NetworkMessageEventHandler? OnMessage;

    public CompoundConnection()
    {
        TcpClient = new TcpClient();
        everReceivedMessages = false;
        firstMessageTimoutTimer = new Timer(_ =>
        {
            if (!everReceivedMessages)
            {
                Trace.TraceError("'Kay time's up!");
                OnFirstMessageTimeout?.Invoke(this, new());
            }
        });
    }
    public CompoundConnection(TcpClient tc, UdpClient uc)
    {
        TcpClient = tc;
        UdpClient = uc;
        everReceivedMessages = false;
        firstMessageTimoutTimer = new Timer(_ =>
        {
            if (!everReceivedMessages)
            {
                Trace.TraceError("'Kay time's up!");
                OnFirstMessageTimeout?.Invoke(this, new());
            }
        });
    }

    public void ConnectUnreliableToEndpoint(IPEndPoint iPEndPoint)
    {
        UdpClient = new UdpClient(AddressFamily.InterNetwork);
        UdpClient.Connect(new IPEndPoint(iPEndPoint.Address.MapToIPv4(), iPEndPoint.Port));
        UdpClient.BeginReceive(HandleUnreliablePacketStart, null);
    }

    public void ResetFirstMessageTimeoutTimer(int timeout)
    {
        firstMessageTimoutTimer.Change(timeout, timeout);
    }
    private void SeeIfConnectionIsStillUp()
    {
        if (!TcpClient.Connected)
        {
            connected = false;
            OnDisconnectedByOtherMeans?.Invoke(this, new());
            return;
        }
    }
    private void HandleReliablePacketStart(IAsyncResult result)
    {
        try
        {
            NetworkStream stream = TcpClient.GetStream();

            if (stream.EndRead(result) != 4)
            {
                SeeIfConnectionIsStillUp();
                return;
            }

            byte[] lengthbytes = (byte[])result.AsyncState!;
            Span<byte> typebytes = stackalloc byte[4];
            int length = BitConverter.ToInt32(lengthbytes);

            if (stream.Read(typebytes) != 4)
            {
                SeeIfConnectionIsStillUp();
                return;
            }

            int type = BitConverter.ToInt32(typebytes);
            byte[] packetbody = new byte[length - 4];

            if (stream.Read(packetbody) != length - 4)
            {
                SeeIfConnectionIsStillUp();
                return;
            }

            stream.BeginRead(lengthbytes, 0, 4, HandleReliablePacketStart, lengthbytes);

            everReceivedMessages = true;

            NetworkPacket packet = new NetworkPacket();
            packet.Type = type;
            packet.Payload = packetbody;

            OnMessage?.Invoke(this, packet);
        }
        catch (Exception ex)
        {
            Trace.TraceError("HandleReliablePacketStart: " + ex.GetType() + ", msg = " + ex.Message + "; disconnecting...");
            OnDisconnectedByOtherMeans?.Invoke(this, new());
            Disconnect();
        }
    }
    private void HandleUnreliablePacketStart(IAsyncResult result)
    {
        if (UdpClient == null)
        {
            Trace.TraceError("Unreliable connection hasn't been established yet, not handling...");
            return;   
        }

        try
        {
            IPEndPoint? sender = null;
            byte[] datagram = UdpClient.EndReceive(result, ref sender);

            sender!.Address = sender.Address.MapToIPv4();

            if (!sender.Equals(UdpClientEndpoint))
            {
                Trace.TraceWarning("Unknown UDP packet coming from " + sender + " who's that?");
                UdpClient.BeginReceive(HandleUnreliablePacketStart, null);
                return;
            }

            using MemoryStream ms = new MemoryStream(datagram);
            using BinaryReader br = new BinaryReader(ms);

            UdpClient.BeginReceive(HandleUnreliablePacketStart, null);

            int type = br.ReadInt32();
            byte[] bytes = br.ReadBytes(datagram.Length - 4);

            everReceivedMessages = true;

            NetworkPacket networkPacket = new NetworkPacket();
            networkPacket.Type = type;
            networkPacket.Payload = bytes;

            OnMessage?.Invoke(this, networkPacket);
        }
        catch (Exception ex)
        {
            Trace.TraceError("HandleUnreliablePacketStart: " + ex.GetType() + ", msg = " + ex.Message + "; disconnecting...");
            OnDisconnectedByOtherMeans?.Invoke(this, new());
            Disconnect();
        }
    }

    public void StartReading()
    {
        NetworkStream networkStream = TcpClient.GetStream();
        byte[] lengthbytes = new byte[4];
        networkStream.BeginRead(lengthbytes, 0, 4, HandleReliablePacketStart, lengthbytes);

        UdpClient?.BeginReceive(HandleUnreliablePacketStart, null);
    }

    public void Connect(IPEndPoint endPoint, int firstMessageTimeout)
    {
        if (connected)
            throw new NetworkException("CompoundConnection already connected!");

        connected = true;

        TcpClient.Connect(endPoint);

        if (!TcpClient.Connected)
        {
            connected = false;
            throw new NetworkException("Couldn't connect to " + endPoint);
        }

        NetworkStream networkStream = TcpClient.GetStream();
        byte[] lengthbytes = new byte[4];
        networkStream.BeginRead(lengthbytes, 0, 4, HandleReliablePacketStart, lengthbytes);

        firstMessageTimoutTimer.Change(firstMessageTimeout, firstMessageTimeout);
    }
    public void SendPacketReliable(NetworkPacket networkPacket)
    {
        try
        {
            NetworkStream networkStream = TcpClient.GetStream();
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(ms);

            bw.Write(networkPacket.Payload.Length + 4);
            bw.Write(networkPacket.Type);
            bw.Write(networkPacket.Payload);

            networkStream.Write(ms.ToArray());
        }
        catch
        {
            Trace.TraceWarning("Reliable packet sending failed spectacularly. Aborting connection..");
            Disconnect();
            OnDisconnectedByOtherMeans?.Invoke(this, new());
        }
    }
    public void SendPacketUnreliable(NetworkPacket networkPacket)
    {
        if (UdpClient == null)
        {
            Trace.TraceError("Unreliable connection hasn't been established yet, not sending...");
            return;   
        }
        
        using MemoryStream ms = new MemoryStream();
        using BinaryWriter bw = new BinaryWriter(ms);

        bw.Write(networkPacket.Type);
        bw.Write(networkPacket.Payload);

        byte[] bytes = ms.ToArray();
        UdpClient.BeginSend(bytes, bytes.Length, UdpClientEndpoint, delegate(IAsyncResult asyncResult)
        {
            try
            {
                UdpClient.EndSend(asyncResult);
            }
            catch
            {
                Trace.TraceWarning("Unreliable packet sending failed spectacularly. Aborting connection..");
                Disconnect();
                OnDisconnectedByOtherMeans?.Invoke(this, new());
            }
        }, 
        null);
    }
    public void Disconnect()
    {
        lock (this)
        {
            if (connected)
            {
                connected = false;
                Trace.TraceInformation("CompoundConnection.Disconnect()");
                TcpClient.Close();
                UdpClient?.Close();
            }
        }
    }

    public void Dispose()
    {
        lock (this)
        {
            if (!disposed)
            {
                disposed = true;
                Trace.TraceInformation("CompoundConnection.Dispose()");
                TcpClient.Dispose();
                UdpClient?.Dispose();
                firstMessageTimoutTimer.Dispose();
            }
        }
    }
}