using NetBlox.Instances;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Server;
using NetBlox.Structs;

namespace NetBlox.Client
{
	public static class Program
	{
        public static void Main(string[] args)
        {
            LogManager.LogInfo($"NetBlox Server ({GameManager.VersionMajor}.{GameManager.VersionMinor}.{GameManager.VersionPatch}) is running...");
            /*GameManager.Start(false, true, false, args, x =>
            {
                DataModel dm = new();
                RbxlParser.Load(x, GameManager.CurrentRoot);

                GameManager.CurrentRoot = dm;

                LuaRuntime.Setup(GameManager.CurrentRoot, false);

                NetworkManager.StartServer();

                GameManager.IsRunning = true;
            });
            return;*/
            GameManager.Start(false, true, false, args, x =>
			{
                DataModel dm = new();
                Workspace ws = new();
                ReplicatedStorage rs = new();
                ReplicatedFirst ri = new();
                RunService ru = new();
                Players pl = new();
                LocalScript ls = new();

                ws.ZoomToExtents();
                ws.Parent = dm;
                dm.Name = "Baseplate";

                Part part = new()
                {
                    Parent = ws,
                    Color = Color.DarkGreen,
                    Position = new(0, -5, 0),
                    Size = new(50, 2, 20),
                    TopSurface = SurfaceType.Studs,
                    Anchored = true
                };

                ls.Parent = ri;
                ls.Source = "print(\"HIIIIII\"); printidentity();";

                rs.Parent = dm;
                ri.Parent = dm;
                pl.Parent = dm;
                ru.Parent = dm;

                GameManager.CurrentIdentity.MaxPlayerCount = 8;
                GameManager.CurrentIdentity.PlaceName = "Default Place";
                GameManager.CurrentIdentity.UniverseName = "NetBlox Defaults";
                GameManager.CurrentIdentity.Author = "The Lord";
                GameManager.CurrentIdentity.PlaceID = 47384;
                GameManager.CurrentIdentity.UniverseID = 47384;

                GameManager.CurrentRoot?.Destroy();
                GameManager.CurrentRoot = dm;

                LuaRuntime.Setup(GameManager.CurrentRoot, false);

                NetworkManager.StartServer();

                GameManager.IsRunning = true;
            });
		}
	}
}