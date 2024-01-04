using RandomNameGeneratorLibrary;
using Services;

using NLog;
using Clients;

/// <summary>
/// Client
/// </summary>
class Client
{
    /// <summary>
    /// Create rabbit object
    /// </summary>
    private readonly RabbitDesc rabbit = new RabbitDesc();

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
                    InitializeRabbit(wolf);
                }

                //loop checking if rabbit is alive, if not make a new one
                while(true)
                {
                    mLog.Info(wolf.IsRabbitAlive(rabbit));
                    while(wolf.IsRabbitAlive(rabbit))
                    {
                        rabbit.DistanceToWolf = rng.Next(1, 100);
                        wolf.UpdateRabbitDistanceToWolf(rabbit);
                        mLog.Info($"The Rabbit is {rabbit.DistanceToWolf}m away");
                        Thread.Sleep(3000);
                    }

                    mLog.Info("Rabbit has died RIP");
                    Thread.Sleep(5000);
                    InitializeRabbit(wolf);
                }
            }
            catch(Exception err)
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
    /// Initialize a rabbit with a random name, weight, id and set a safe distance
    /// And add it to the wolf area
    /// </summary>
    /// <param name="wolf">Wolf service for accessing the methods required</param>
    private void InitializeRabbit(IWolfService wolf)
    {
        var personGenerator = new PersonNameGenerator();

        rabbit.RabbitName = personGenerator.GenerateRandomFirstAndLastName();
        rabbit.Weight = rng.Next(0, 10);
        rabbit.isRabbitAlive = true;
        rabbit.DistanceToWolf = 1000;
        rabbit.RabbitID = wolf.EnterWolfArea(rabbit);

        mLog.Info($"{rabbit.RabbitName} ({rabbit.Weight}) the Rabbit is born! #{rabbit.RabbitID}");
    }

    
}