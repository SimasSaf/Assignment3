using Services;
using NLog;

/// <summary>
/// Wolf state descritor
/// </summary>
public class WolfState
{
    /// <summary>
	/// Access lock.
	/// </summary>
    public readonly object AccessLock = new object();

	/// <summary>
	/// Last unique ID value generated.
	/// </summary>
    public int LastUniqueID;

    /// <summary>
    /// Coordinates for wold location
    /// </summary>
    public int x;
    public int y;

    /// <summary>
    /// Wolf weight
    /// </summary>
    public int WolfWeight;

    /// <summary>
    /// List of Rabbit objects nearby
    /// </summary>
    public List<RabbitDesc> RabbitsNearby = new List<RabbitDesc>();

    /// <summary>
    /// List of Water objects nearby
    /// </summary>
    public List<WaterDesc> WaterNearby = new List<WaterDesc>();
}

/// <summary>
/// Wolf logic
/// </summary>
class WolfLogic : IWolfService
{
    /// <summary>
    /// Weight for when the wolf has to reset its weight
    /// </summary>
    static readonly int WOLF_MAX_WEIGHT = 30;

    /// <summary>
    /// Initialization of wolf being not full until reaching WOLF_MAX_WEIGHT
    /// </summary>
    static bool WOLF_IS_FULL = false;

    /// <summary>
    /// Background task thread
    /// </summary>
    private Thread backgroundTaskThread;

    /// <summary>
    /// Logger for the class
    /// </summary>
    private Logger mLog = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// State descriptor
    /// </summary>
    private WolfState wolfState = new WolfState();

    /// <summary>
    /// Random number generator
    /// </summary>
    Random rng = new Random();

    /// <summary>
    /// Constructor
    /// </summary>
    public WolfLogic()
    {
        //start background task
        backgroundTaskThread = new Thread(BackgroundTask);
        backgroundTaskThread.Start();
    }

    /// <summary>
    /// Add rabbit to the RabbitsNearby list for the wolf to consume
    /// </summary>
    /// <param name="rabbit">Initialized rabbit object to be added</param>
    /// <returns>Rabbit Id</returns>
    public int EnterWolfArea(RabbitDesc rabbit)
    {
        mLog.Info("A Rabbit has entered the Wolf area");

        lock(wolfState.AccessLock)
        {
            wolfState.LastUniqueID += 1;
            rabbit.RabbitID = wolfState.LastUniqueID;
            wolfState.RabbitsNearby.Add(rabbit);

            return rabbit.RabbitID;
        }
    }

    /// <summary>
    /// Add Water object to WaterNearby list for the wolf to consume
    /// </summary>
    /// <param name="water">Initialized water object to be added to the list</param>
    /// <returns>Rabbit Id</returns>
    public int SpawnWaterNearWolf(WaterDesc water)
    {
        mLog.Info("~~~ Spawning Water near Wolf ~~~");

        lock(wolfState.AccessLock)
        {
            wolfState.LastUniqueID += 1;
            water.WaterID = wolfState.LastUniqueID;
            wolfState.WaterNearby.Add(water);

            return water.WaterID;
        }
    }

    /// <summary>
    /// Updates the local rabbits distance to wolf in the rabbitsNearby list
    /// </summary>
    /// <param name="rabbit">Rabbit object to update</param>
    /// <returns>True if success, False if not</returns>
    public bool UpdateRabbitDistanceToWolf(RabbitDesc rabbit)
    {
        lock(wolfState.AccessLock)
        {
            mLog.Info("Updating rabbit distance " + rabbit.DistanceToWolf);
            var rabbitNearby = wolfState.RabbitsNearby.Find(r => r.RabbitID.Equals(rabbit.RabbitID));

            if (rabbitNearby != null)
            {
                rabbitNearby.DistanceToWolf = rabbit.DistanceToWolf;
                return true; // Update successful
            }
            else
            {
                return false; // Rabbit not found, update failed
            }
        }
    }

    /// <summary>
    /// Checks if specific rabbit still exists in the list
    /// </summary>
    /// <param name="rabbit">Rabbit to check if exists in list</param>
    /// <returns>True if exists, false if doesnt</returns>
    public bool IsRabbitAlive(RabbitDesc rabbit)
    {
        lock(wolfState.AccessLock)
        {
            return wolfState.RabbitsNearby.Any(rabbitNearby => rabbitNearby.RabbitID == rabbit.RabbitID);
        }
    }

    /// <summary>
    /// Check if specific water still exists in the list
    /// </summary>
    /// <param name="water">Water that is checked if still exists in list</param>
    /// <returns>True if exists, false if not</returns>
    public bool IsWaterAlive(WaterDesc water)
    {
        lock(wolfState.AccessLock)
        {
            return wolfState.WaterNearby.Any(waterNearby => waterNearby.WaterID == water.WaterID);
        }
    }

    /// <summary>
    /// Background task for wolf
    /// </summary>
    private void BackgroundTask()
    {
        while(true)
        {
            lock(wolfState.AccessLock)
            {
                mLog.Info($"The wolf ({wolfState.WolfWeight}) is moving...");

                GenerateRandomWolfCoordinates();
                
                mLog.Info($"The Wolf is currently at [{wolfState.x},{wolfState.y}]");
                
                //This is just to not spam, let's imagine its doing calculations :D
                Thread.Sleep(1000);

                CheckRabbitsNearby();

                CheckWaterNearby();
            };

            if (WOLF_IS_FULL)
            {
                lock(wolfState.AccessLock)
                {
                    mLog.Info("@@@ Wolf is Full");
                    wolfState.WolfWeight = 0;
                }

                Thread.Sleep(5000);
                WOLF_IS_FULL = false;
                mLog.Info("@@@ Wolf is no longer Full");
            }
        }
    }

    /// <summary>
    /// Check if there is a rabbit nearby that can be eaten
    /// </summary>
    private void CheckRabbitsNearby()
    {
        List<RabbitDesc> newRabbitsNearby = wolfState.RabbitsNearby;

        for (int i = newRabbitsNearby.Count - 1; i >= 0; i--)
        {
            if (wolfState.WolfWeight < WOLF_MAX_WEIGHT)
            {
                mLog.Info("!!! Wolf is sniffing out the rabbits... !!!");

                var rabbit = wolfState.RabbitsNearby[i];
                mLog.Info("Rabbit distance: " + rabbit.DistanceToWolf);

                if (rabbit.DistanceToWolf <= 30)
                {
                    mLog.Info("Rabbit distance: " + rabbit.DistanceToWolf);

                    EatRabbit(rabbit);
                }
            }
            else
            {
                WOLF_IS_FULL = true;
                break;
            }
        }
    }

    /// <summary>
    /// Check if water exists close enough for the wolf to consume
    /// </summary>
    private void CheckWaterNearby()
    {
        List<WaterDesc> newWaterNearby = wolfState.WaterNearby;

        for (int i = newWaterNearby.Count - 1; i >= 0; i--)
        {
            if (wolfState.WolfWeight < WOLF_MAX_WEIGHT)
            {
                mLog.Info("Wolf is looking for Water...");

                var water = wolfState.WaterNearby[i];

                if (Math.Abs(wolfState.x - water.x) <= 5 || Math.Abs(wolfState.y - water.y) <= 5)
                {
                    DrinkWater(water);
                }
            }
            else
            {
                WOLF_IS_FULL = true;
                break;
            }
        }
    }

    /// <summary>
    /// Consume rabbit weight to wolf weight and remove from the nearby list
    /// </summary>
    /// <param name="rabbit">Rabbit object that needs to be removed</param>
    private void EatRabbit(RabbitDesc rabbit)
    {
        mLog.Info($"Eating {rabbit.RabbitName} The Rabbit");
        wolfState.WolfWeight += rabbit.Weight;
        wolfState.RabbitsNearby.Remove(rabbit);
    }

    /// <summary>
    /// Consume water weight to wolf weight and remove the water from the nearby list
    /// </summary>
    /// <param name="water">Water object that needs to be removed</param>
    private void DrinkWater(WaterDesc water)
    {
        mLog.Info("Drinking water...");
        wolfState.WolfWeight += water.Volume;
        wolfState.WaterNearby.Remove(water);
    }

    /// <summary>
    /// Generates random wolf coordinates
    /// </summary>
    private void GenerateRandomWolfCoordinates()
    {
        wolfState.x = rng.Next(-50, 50);
        wolfState.y = rng.Next(-50, 50);
    }
}

