using Services;
using NLog;

using Clients;

/// <summary>
/// Client
/// </summary>
class Client
{
    /// <summary>
    /// Create water object
    /// </summary>
    private readonly WaterDesc water = new WaterDesc();

    /// <summary>
    /// Random number generator init
    /// </summary>
    private readonly Random rng = new Random();

    /// <summary>
    /// Get logger for this class
    /// </summary>
    Logger mLog = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Configure logging system
    /// </summary>
	private void ConfigureLogging()
	{
		var config = new NLog.Config.LoggingConfiguration();

		var console =
			new NLog.Targets.ConsoleTarget("console")
			{
				Layout = @"${date:format=HH\:mm\:ss}|${level}| ${message} ${exception}"
			};
		config.AddTarget(console);
		config.AddRuleForAllLevels(console);

		LogManager.Configuration = config;
	}

    /// <summary>
    /// Porgram body
    /// </summary>
    private void Run()
    {
        //configure logging
        ConfigureLogging();

		//run everythin in a loop to recover from connection errors
        while(true)
        {
            try
            {
                //connect to the server, get service client proxy
                var wolf = new WolfClient();

                //initialize rabbit for the wolf if wolf exists
                if(wolf != null)
                {
                    InitializeWater(wolf);
                }

                //check if water is alive and if not, initialize a new one after some time
                while(true)
                {
                    while(wolf.IsWaterAlive(water))
                    {
                        mLog.Info("~~~~~~~~~~~~~~~~~");
                        //Checks every 0.5s
                        Thread.Sleep(500);
                    }

                    mLog.Info("The water is empty");
                    Thread.Sleep(5000);
                    InitializeWater(wolf);
                }

            }
            catch (Exception err)
            {
                mLog.Error("Error has occured...", err);
                Thread.Sleep(3000);
            }
        }
    }

	/// <summary>
	/// Program entry point.
	/// </summary>
	/// <param name="args">Command line arguments.</param>
    static void Main(string[] args)
	{
		var self = new Client();
		self.Run();
	}

    /// <summary>
    /// Initialize the water object with random volume and coordinates
    /// Add it to the wold arean
    /// </summary>
    /// <param name="wolf">Wolf service for accessing the methods required</param>
    private void InitializeWater(IWolfService wolf)
    {
        water.Volume = rng.Next(0, 10);
        water.x = rng.Next(-50, 50);
        water.y = rng.Next(-50, 50);
        water.WaterID = wolf.SpawnWaterNearWolf(water);
    }
}