using LetsChess_Backend.Logic;
using LetsChess_Backend.WSHub;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace LetsChess_Backend.WSClients
{
	public class MatchFoundEventArgs {
		public string UserId { get; set; }
		public string MatchId { get; set; }
	}

	public class MatchmakingClient: IDisposable
	{
		private readonly ILogger<MatchmakingClient> logger;
		private readonly IHubContext<MatchHub, IMatchHub> matchHub;
		private readonly ConnectionFactory factory;
		private IConnection connection;
		private IModel channel;
		private EventingBasicConsumer consumer;

		public MatchmakingClient(ILogger<MatchmakingClient> logger, IOptions<ServiceEndpoints> serviceUrls, IHubContext<MatchHub, IMatchHub> matchHub, IOptions<Credentials> mqCredentials)
		{
			this.logger = logger;
			this.matchHub = matchHub;
			
			factory = new ConnectionFactory
			{
				UserName = mqCredentials.Value.Username,
				Password = mqCredentials.Value.Password,
				Endpoint = new AmqpTcpEndpoint(new Uri(serviceUrls.Value.MQ))
			};

			Connect();
		}

		private void Connect() {

			while (true)
			{
				logger.LogDebug("connecting to MQ");
				try
				{
					connection = factory.CreateConnection();
					connection.ConnectionShutdown += Connection_ConnectionShutdown;

					channel = connection.CreateModel();

					consumer = new EventingBasicConsumer(channel);
					consumer.Received += OnMatchFound;

					channel.ExchangeDeclare("matchmaking", ExchangeType.Direct);
					var args = new Dictionary<string, object>
					{
						{ "x-message-ttl", 10000 }
					};
					channel.QueueDeclare("matchmaking", durable: false, exclusive: false, autoDelete: false, arguments: args);
					channel.QueueBind("matchmaking", "matchmaking", "matchmaking");
					channel.BasicConsume("matchmaking", true, consumer);

					logger.LogDebug("succesfully connected to MQ");
					break;
				}
				catch (BrokerUnreachableException ex)
				{
					logger.LogError(ex, $"Could not connect to the service, '{ex.Message}' see {ex.HelpLink} for more details");

					Thread.Sleep(5000);
				}
				catch (Exception ex) {
					logger.LogError(ex, $"An error occured while connecting to the service {ex.Message}");

					Thread.Sleep(5000);
				}
			}
		}

		private void Connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
		{
			logger.LogInformation($"Connection to the MQ has been shutdown with reason: <{e.ReplyCode}> '{e.ReplyText}'");
			connection.ConnectionShutdown -= Connection_ConnectionShutdown;

			Connect();
		}

		private void OnMatchFound(object sender, BasicDeliverEventArgs mess)
		{
			try
			{
				logger.LogDebug($"message received on {mess.RoutingKey}");

				var body = Encoding.UTF8.GetString(mess.Body.ToArray());
				var data = JsonConvert.DeserializeObject<MatchFoundEventArgs>(body);

				matchHub.Clients.All.MatchFound(data.MatchId, data.UserId);
				//TODO: only ack if a client exists with this userId
				channel.BasicAck(mess.DeliveryTag, false);
			}
			catch (Exception ex) {
				logger.LogError(ex, $"failed to parse MQ message on {mess.RoutingKey}");
			}
		}

		public void Dispose()
		{
			connection?.Dispose();
			channel?.Dispose();
		}
	}
}
