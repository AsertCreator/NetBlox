namespace NetBlox.Server
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            GameManager gameManager = new GameManager(Network.NetworkMode.Server);
            gameManager.AddConsoleArguments(args);
            gameManager.InitializeRendering();
            gameManager.LoadPlaceFromDefaults(0);
            gameManager.BeginInitializationPhase();
            gameManager.BeginAlivePhase();
            gameManager.GameScheduler.EnterRunLoop();
        }
    }
}