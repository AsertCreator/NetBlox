namespace NetBlox.Server
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            GameManager gameManager = new GameManager(Network.NetworkMode.Server);
            gameManager.AddConsoleArguments(args);
            if (args.Contains("-Graphics"))
                gameManager.InitializeRendering();
            if (!args.Contains("-Crossroads"))
                gameManager.LoadPlaceFromDefaults(0);
            else
                gameManager.LoadPlaceFromFile("net-network://http://127.0.0.1:8000/places/Crossroads.rbxlx");
            gameManager.BeginInitializationPhase();
            gameManager.BeginAlivePhase();
            gameManager.GameScheduler.EnterRunLoop();
        }
    }
}