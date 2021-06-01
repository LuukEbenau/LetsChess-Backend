
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LetsChess_Backend.Logic
{
	public abstract class MQConnectorBase: IDisposable
	{
		private readonly ConnectionFactory _factory;
		protected IConnection Connection { get; private set; }
		protected IModel Channel { get; private set; }
		public IModel GameChannel { get; private set; }

		protected readonly ILogger<MQConnector> logger;

		public MQConnectorBase(IOptions<ConnectionStrings> connectionStrings, ILogger<MQConnector> logger, IOptions<Credentials> mqCredentials)
		{
			this.logger = logger;

			_factory = new ConnectionFactory()
			{
				Endpoint = new AmqpTcpEndpoint(new Uri(connectionStrings.Value.MQ)),
				UserName = mqCredentials.Value.Username,
				Password = mqCredentials.Value.Password,
			};

			Connect();
		}

		abstract protected void OnMatchFound(object sender, BasicDeliverEventArgs mess);
		abstract protected void OnMoveTaken(object sender, BasicDeliverEventArgs mess);

		protected bool Connect()
		{
			logger.LogDebug("connecting to MQ");
			try
			{
				Connection = _factory.CreateConnection();
				Connection.ConnectionShutdown += Connection_ConnectionShutdown;
				Connection.ConnectionBlocked += Connection_ConnectionBlocked;
				Connection.CallbackException += Connection_CallbackException;
				//Matchmaking
				Channel = Connection.CreateModel();
				Channel.ModelShutdown += Channel_ModelShutdown;
				var matchConsumer = new EventingBasicConsumer(Channel);
				matchConsumer.Received += OnMatchFound;

				Channel.ExchangeDeclare("matchmaking", ExchangeType.Direct);
				var args = new Dictionary<string, object>
					{
						{ "x-message-ttl", 10000 }
					};
				Channel.QueueDeclare("matchmaking", durable: false, exclusive: false, autoDelete: false, arguments: args);
				Channel.QueueBind("matchmaking", "matchmaking", "matchmaking");
				Channel.BasicConsume("matchmaking", false, matchConsumer);

				//Game
				var gameConsumer = new EventingBasicConsumer(Channel);
				gameConsumer.Received += OnMoveTaken;

				Channel.ExchangeDeclare("game", ExchangeType.Direct);
				Channel.QueueDeclare("game", durable: false, exclusive: false, autoDelete: false, arguments: new Dictionary<string, object>
				{
					{ "x-message-ttl", 30000 }
				});
				Channel.QueueBind("game", "game", "game");
				Channel.BasicConsume("game", false, gameConsumer);

				logger.LogDebug("succesfully connected to MQ");
				return true;
			}
			catch (BrokerUnreachableException ex)
			{
				logger.LogError(ex, $"Could not connect to the service, '{ex.Message}' see {ex.HelpLink} for more details");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, $"An error occured while connecting to the service {ex.Message}");
			}
			return false;
		}

		private void Channel_ModelShutdown(object sender, ShutdownEventArgs e)
		{
			logger.LogWarning($"channel destroyed: '{e.ReplyText}'");
		}

		private void Connection_CallbackException(object sender, CallbackExceptionEventArgs e)
		{
			logger.LogError(e.Exception, e.Exception.Message);
		}

		private void Connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
		{
			logger.LogInformation($"Connection to the MQ has been shutdown with reason: <{e.ReplyCode}> '{e.ReplyText}'");
			Connection.ConnectionShutdown -= Connection_ConnectionShutdown;

			Connect();
		}
		private void Connection_ConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
		{
			logger.LogWarning($"Connection got blocked for reason: '{e.Reason}'");
		}

		public void Dispose()
		{
			Connection.Dispose();
			Channel.Dispose();
		}
	}
}
