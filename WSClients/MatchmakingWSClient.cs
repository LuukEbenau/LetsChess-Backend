using LetsChess_Backend.Logic;
using LetsChess_Backend.WSHub;

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System;
using System.Threading.Tasks;

namespace LetsChess_Backend.WSClients
{
	public class MatchFoundEventArgs {
		public string UserId { get; set; }
		public string MatchId { get; set; }
	}

	public class MatchmakingWSClient
	{
		private readonly ILogger<MatchmakingWSClient> logger;
		private readonly IHubContext<MatchHub, IMatchHub> matchHub;
		private readonly HubConnection connection;

		public MatchmakingWSClient(ILogger<MatchmakingWSClient> logger, IOptions<ServiceEndpoints> serviceUrls, IHubContext<MatchHub, IMatchHub> matchHub) {
			this.logger = logger;
			this.matchHub = matchHub;
			connection = new HubConnectionBuilder()
				.WithUrl($"{serviceUrls.Value.MatchmakingService}/hub")
				.Build();
			connection.Closed += Connection_Closed;
			try
			{
				this.logger.LogDebug($"connecting to MatchmakingWSClient...");
				connection.StartAsync().ContinueWith(e => {
					this.logger.LogDebug("connected to MatchmakingWSClient");
				});
			}
			catch (Exception ex) {
				this.logger.LogError(ex, $"failed to connect to MatchmakingWSClient with error: '{ex.Message}'");
			}
			
			connection.On<string,string>("MatchFound", (matchId, userId) =>
			{
				this.logger.LogDebug($"Match found message received for match '{matchId}' and user '{userId}'", matchId, userId);
				this.matchHub.Clients.All.MatchFound(matchId, userId);
				//TODO: only specific users
			});
		}

		private async Task Connection_Closed(Exception ex)
		{
			logger.LogWarning(ex, $"websocket connection closed to MatchmakingWSClient, retrying soon... '{ex.Message}'");
			await Task.Delay(new Random().Next(0, 5) * 1000);
			logger.LogDebug(ex, $"trying to reinitialize websocket connection to MatchmakingWSClient '{ex.Message}'");
			await connection.StartAsync();
		}
	}
}
