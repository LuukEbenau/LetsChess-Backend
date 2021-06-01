using LetsChess.Models.Messages;

using LetsChess_Backend.Models;
using LetsChess_Backend.WSHub;

using LetsChess_Models.Models;

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

namespace LetsChess_Backend.Logic
{

	public class MQConnector: MQConnectorBase
	{
		private readonly IHubContext<MatchHub, IMatchHub> matchHub;

		public MQConnector(IOptions<ConnectionStrings> connectionStrings, ILogger<MQConnector> logger, IOptions<Credentials> mqCredentials, IHubContext<MatchHub, IMatchHub> matchHub) : base(connectionStrings, logger, mqCredentials)
		{
			this.matchHub = matchHub;
		}

		protected override void OnMatchFound(object sender, BasicDeliverEventArgs mess)
		{
			try
			{
				logger.LogDebug($"message received on {mess.RoutingKey}");

				var body = Encoding.UTF8.GetString(mess.Body.ToArray());
				var data = JsonConvert.DeserializeObject<MatchFoundMessage>(body);

				matchHub.Clients.All.MatchFound(data);
				//TODO: only ack if a client exists with this userId
				Channel.BasicAck(mess.DeliveryTag, false);
			}
			catch (Exception ex) {
				logger.LogError(ex, $"failed to parse MQ message on {mess.RoutingKey}");
			}
		}

		protected override void OnMoveTaken(object sender, BasicDeliverEventArgs mess)
		{
			try
			{
				logger.LogDebug($"message received on {mess.RoutingKey}");

				var body = Encoding.UTF8.GetString(mess.Body.ToArray());
				var data = JsonConvert.DeserializeObject<TakeMoveMessage>(body);

				matchHub.Clients.All.MoveTaken(data);
				//TODO: only ack if a client exists with this userId
				Channel.BasicAck(mess.DeliveryTag, false);
				
			}
			catch (Exception ex)
			{
				logger.LogError(ex, $"failed to parse MQ message on {mess.RoutingKey}");
			}
		}
	}
}
