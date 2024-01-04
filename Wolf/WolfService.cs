using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;

using System.Text;

using Services;


/// <summary>
/// Service
/// </summary>
public class WolfService
{
	/// <summary>
	/// Name of the request exchange.
	/// </summary>
	private static readonly String ExchangeName = "Wolf.Exchange";

	/// <summary>
	/// Name of the request queue.
	/// </summary>
	private static readonly String ServerQueueName = "Wolf.WolfService";

	/// <summary>
	/// Logger for this class.
	/// </summary>
	private Logger log = LogManager.GetCurrentClassLogger();

    /// <summary>
	/// Connection to RabbitMQ message broker.
	/// </summary>
	private IConnection rmqConn;

    /// <summary>
	/// Communications channel to RabbitMQ message broker.
	/// </summary>
	private IModel rmqChann;

	/// <summary>
	/// Service logic.
	/// </summary>
	private WolfLogic logic = new WolfLogic();

	/// <summary>
	/// Constructor.
	/// </summary>
	public WolfService()
	{
		//connect to the RabbitMQ message broker
		var rmqConnFact = new ConnectionFactory();
		rmqConn = rmqConnFact.CreateConnection();

		//get channel, configure exchanges and request queue
		rmqChann = rmqConn.CreateModel();

		rmqChann.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Direct);
		rmqChann.QueueDeclare(queue: ServerQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
		rmqChann.QueueBind(queue: ServerQueueName, exchange: ExchangeName, routingKey: ServerQueueName, arguments: null);

		//connect to the queue as consumer
		var rmqConsumer = new EventingBasicConsumer(rmqChann);
		rmqConsumer.Received += (consumer, delivery) => OnMessageReceived(((EventingBasicConsumer)consumer).Model, delivery);
		rmqChann.BasicConsume(queue: ServerQueueName, autoAck: true, consumer : rmqConsumer);
	}

    /// <summary>
	/// Is invoked to process messages received.
	/// </summary>
	/// <param name="channel">Related communications channel.</param>
	/// <param name="msgIn">Message deliver data.</param>
	private void OnMessageReceived(IModel channel, BasicDeliverEventArgs msgIn)
	{
		try
		{
			//get call request
			var request =
				JsonConvert.DeserializeObject<RPCMessage>(
					Encoding.UTF8.GetString(
						msgIn.Body.ToArray()
					)
				);

			//set response as undefined by default
			RPCMessage response = null;

			//process the call
			switch( request.Action )
			{
				case $"Call_{nameof(logic.EnterWolfArea)}":
				{
                    //deserialize arguments
                    var rabbit = JsonConvert.DeserializeObject<RabbitDesc>(request.Data);

					//make the call
					var result = logic.EnterWolfArea(rabbit);

					//create response
					response =
						new RPCMessage() {
							Action = $"Result_{nameof(logic.EnterWolfArea)}",
							Data = JsonConvert.SerializeObject(new {Value = result})
						};

					//
					break;
				}

				case $"Call_{nameof(logic.SpawnWaterNearWolf)}":
				{
                    //deserialize arguments
                    var water = JsonConvert.DeserializeObject<WaterDesc>(request.Data);

					//make the call
					var result = logic.SpawnWaterNearWolf(water);

					//create response
					response =
						new RPCMessage() {
							Action = $"Result_{nameof(logic.SpawnWaterNearWolf)}",
							Data = JsonConvert.SerializeObject(new {Value = result})
						};

					//
					break;
				}

				case $"Call_{nameof(logic.UpdateRabbitDistanceToWolf)}":
				{
					//deserialize arguments
					var rabbit = JsonConvert.DeserializeObject<RabbitDesc>(request.Data);

					//make the call
					var result = logic.UpdateRabbitDistanceToWolf(rabbit);

					//create response
					response =
						new RPCMessage() {
							Action = $"Result_{nameof(logic.UpdateRabbitDistanceToWolf)}",
							Data = JsonConvert.SerializeObject(new {Value = result})
						};

					//
					break;
				}

				case $"Call_{nameof(logic.IsRabbitAlive)}":
				{
					//deserialize arguments
                    var rabbit = JsonConvert.DeserializeObject<RabbitDesc>(request.Data);

					//make the call
					var result = logic.IsRabbitAlive(rabbit);

					//create response
					response =
						new RPCMessage() {
							Action = $"Result_{nameof(logic.IsRabbitAlive)}",
							Data = JsonConvert.SerializeObject(new {Value = result})
						};

					//
					break;
				}

				case $"Call_{nameof(logic.IsWaterAlive)}":
				{
					//deserialize arguments
					var water = JsonConvert.DeserializeObject<WaterDesc>(request.Data);

					//make the call
					var result = logic.IsWaterAlive(water);

					//create response
					response =
						new RPCMessage() {
							Action = $"Result_{nameof(logic.IsWaterAlive)}",
							Data = JsonConvert.SerializeObject(result)
						};

					//
					break;
				}

				default:
				{
					log.Info($"Unsupported type of RPC action '{request.Action}'. Ignoring the message.");
					break;
				}
			}

			//response is defined? send reply message
			if( response != null )
			{
				//prepare metadata for outgoing message
				var msgOutProps = channel.CreateBasicProperties();
				msgOutProps.CorrelationId = msgIn.BasicProperties.CorrelationId;

				//send reply message to the client queue
				channel.BasicPublish(
					exchange : ExchangeName,
					routingKey : msgIn.BasicProperties.ReplyTo,
					basicProperties : msgOutProps,
					body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response))
				);
			}
		}
		catch( Exception e )
		{
			log.Error(e, "Unhandled exception caught when processing a message. The message is now lost.");
		}
	}	
}