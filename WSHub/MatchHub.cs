using LetsChess.Models.Messages;

using LetsChess_Models.Models;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace LetsChess_Backend.WSHub
{
	public interface IMatchHub {
		Task MatchFound(MatchFoundMessage mess);
		Task MoveTaken(TakeMoveMessage mess);
	}
	public class MatchHub : Hub<IMatchHub>
	{

		public MatchHub() {

		}
	}
}
