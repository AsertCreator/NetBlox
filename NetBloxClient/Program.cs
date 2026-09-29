namespace NetBlox.Server
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Contains("-Pause"))
            {
                Console.Write("Ready whenever you are...");
                Console.Read();
            }

            GameManager gameManager = new GameManager(Network.NetworkMode.Client);
            gameManager.AddConsoleArguments(args);
            gameManager.InitializeRendering();
            gameManager.LoadLoadingPlace();
            gameManager.BeginInitializationPhase();
            gameManager.BeginAlivePhase();
            gameManager.GameScheduler.EnterRunLoop();
        }
    }
}