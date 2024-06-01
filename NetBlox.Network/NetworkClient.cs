using NetBlox.Runtime;
using Network;
using Network.Enums;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace NetBlox.Network
{
	public class NetworkClient
	{
		public NetworkManager NetworkManager;
		public Connection RemoteConnection;
		public RemoteClient Self;

		public NetworkClient(NetworkManager nm) 
		{ 
			NetworkManager = nm;
		}
		public void DisconnectFromServer(CloseReason cr)
		{
			Self.Close();
			LogManager.LogInfo("Disconnected from server due to " + cr + "!");

			GameManager.CurrentRoot.GetService<ReplicatedFirst>().Destroy();
			GameManager.CurrentRoot.GetService<Players>().Destroy();
			GameManager.CurrentRoot.GetService<Workspace>().Destroy();
			GameManager.CurrentRoot.GetService<ReplicatedStorage>().Destroy();
		}
		public void Start(IPAddress ipa, int port)
		{
			if (NetworkManager.IsServer)
				throw new NotSupportedException("Cannot teleport in server");

			Task.Run(() =>
			{
				NetworkManager.GameManager.RenderManager!.Status = string.Empty;

				try
				{
					LogManager.LogInfo($"Teleporting into server: {ipa}...");
					NetworkManager.GameManager.CurrentIdentity.Reset();
					LuaRuntime.Threads.Clear();

					try
					{
						LogManager.LogInfo("Starting client network thread...");

						var res = ConnectionResult.TCPConnectionNotAlive;
						var con = ConnectionFactory.CreateTcpConnection(ipa.ToString(), port, out res);

						RemoteConnection = con;
						con.EnableLogging = false;

						if (res == ConnectionResult.Connected)
						{
							LogManager.LogInfo($"Connected to {ipa}, performing C>S handshake...");
							con.ConnectionClosed += (x, y) =>
							{
								DisconnectFromServer(x);
							};

							ServerHandshake sh = default;
							ClientHandshake ch = new();
							ch.Username = GameManager.Username;
							ch.VersionMajor = AppManager.VersionMajor;
							ch.VersionMinor = AppManager.VersionMinor;
							ch.VersionPatch = AppManager.VersionPatch;

							con.RegisterRawDataHandler("nb.inc-inst", (x, y) =>
							{
								var ins = SeqReceiveInstance(y, x.ToUTF8String());
								if (ins.UniqueID == sh.DataModelInstance)
								{
									GameManager.CurrentRoot.Name = (ins as DataModel)!.Name;
									GameManager.IsRunning = true;

									con.RegisterRawDataHandler("nb.repar-inst", (x, y) =>
									{
										var dss = DeserializeJsonBytes<Dictionary<string, string>>(x.Data);
										var ins = GameManager.GetInstance(Guid.Parse(dss["Instance"]));
										var par = GameManager.GetInstance(Guid.Parse(dss["Parent"]));

										if (ins == null || par == null)
										{
											LogManager.LogError("Failed to reparent instance, because new parent does not exist");
											return;
										}
										else
										{
											ins.Parent = par;
										}
									});
								}
								if (ins.UniqueID == sh.PlayerInstance)
								{
									var plr = (Player)ins;
									var pls = GameManager.CurrentRoot.GetService<Players>();
									plr.IsLocalPlayer = true;
									pls.LocalPlayer = plr;
								}
								if (ins.UniqueID == sh.CharacterInstance)
								{
									var chr = (Character)ins;
									chr.IsLocalPlayer = true;
									var cam = new Camera(GameManager);
									cam.Parent = GameManager.CurrentRoot.GetService<Workspace>();
									cam.CameraSubject = chr;
								}
								if (loaded++ == sh.InstanceCount)
								{
									GameManager.CurrentRoot.GetService<CoreGui>().HideTeleportGui();
								}
							});
							con.RegisterRawDataHandler("nb.inc-service", (x, y) =>
							{
								var ins = SeqReceiveInstance(y, x.ToUTF8String());
								ins.Parent = GameManager.CurrentRoot;
							});
							con.RegisterRawDataHandler("nb.placeinfo", (x, y) =>
							{
								sh = DeserializeJsonBytes<ServerHandshake>(x.Data);

								if (sh.ErrorCode != 0)
								{
									var msg = $"Could not connect to the server! {sh.ErrorCode} - {sh.ErrorMessage}";
									LogManager.LogError(msg);
									GameManager.RenderManager.Status = msg;
									return;
								}

								GameManager.CurrentIdentity.PlaceName = sh.PlaceName;
								GameManager.CurrentIdentity.PlaceID = sh.PlaceID;
								GameManager.CurrentIdentity.UniverseName = sh.UniverseName;
								GameManager.CurrentIdentity.UniverseID = sh.UniverseID;
								GameManager.CurrentIdentity.Author = sh.Author;
								GameManager.CurrentIdentity.UniquePlayerID = sh.UniquePlayerID;
								GameManager.CurrentIdentity.MaxPlayerCount = sh.MaxPlayerCount;

								GameManager.CurrentRoot.GetService<CoreGui>().ShowTeleportGui(sh.PlaceName, sh.Author, (int)sh.PlaceID, 0);

								y.SendRawData("nb.req-int-rep", []);
							});

							con.SendRawData("nb.handshake", SerializeJsonBytes(ch));
						}
					}
					catch (Exception ex)
					{
						var msg = $"Could not connect to the server! {ex.GetType().Name} - {ex.Message}";
						var pls = GameManager.CurrentRoot.GetService<Players>();
						if (pls == null) return;
						var plr = pls.LocalPlayer as Player;

						LogManager.LogError(msg);

						plr?.Kick(msg);

						return;
					}
				}
				catch (Exception ex)
				{
					GameManager.RenderManager.Status = $"Could not connect due to error! {ex.GetType().FullName}: {ex.Message}";
					LogManager.LogError(GameManager.RenderManager.Status);
				}
			});
		}
	}
}
