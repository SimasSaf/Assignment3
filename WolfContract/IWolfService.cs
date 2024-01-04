namespace Services;

public class RabbitDesc
{
	public int RabbitID { get; set; }
	public string RabbitName { get; set; }
	public int Weight { get; set; }

	public int DistanceToWolf { get; set; }

	public bool isRabbitAlive { get; set; }
}

public class WaterDesc
{
	public int WaterID { get; set; }
	public int Volume { get; set; }

	public int x { get; set; }
	public int y { get; set; }
}

public interface IWolfService
{
	int EnterWolfArea(RabbitDesc rabbit);

	int SpawnWaterNearWolf(WaterDesc water);

	bool UpdateRabbitDistanceToWolf(RabbitDesc rabbit);

	bool IsRabbitAlive(RabbitDesc rabbit);

	bool IsWaterAlive(WaterDesc water);
}