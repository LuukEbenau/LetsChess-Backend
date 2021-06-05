
using LetsChess_Backend.Messages;

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
