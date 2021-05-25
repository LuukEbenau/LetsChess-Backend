using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace LetsChess_Backend.WSHub
{
	public interface IMatchHub {
		Task MatchFound(string matchId, string userId);
	}
	public class MatchHub : Hub<IMatchHub>
	{
		private readonly ILogger<MatchHub> logger;
		public MatchHub(ILogger<MatchHub> logger) {
			this.logger = logger;	
		}
	}
}
