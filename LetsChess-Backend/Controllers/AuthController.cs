using LetsChess_Backend.Logic;
using LetsChess_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace LetsChess_Backend.Controllers
{
	[Route("auth")]
	[ApiController]
	public class AuthController : ControllerBase
	{
		private readonly GoogleConnector authConnector;
		private readonly UserServiceConnector userServiceConnector;
		private readonly ILogger<AuthController> logger;

		public AuthController(IOptions<AuthSettings> authSettings, IOptions<ServiceEndpoints> serviceEndpoints, ILogger<AuthController> logger)
		{
			authConnector = new GoogleConnector(authSettings.Value.ClientId);
			userServiceConnector = new UserServiceConnector(serviceEndpoints?.Value?.UserService);

			this.logger = logger;
		}

		[HttpGet("redirectToIdentity")]
		public IActionResult RedirectToIdentity(string redirectUrl)
		{
			if (redirectUrl == default)
			{
				var responseMessage = "the Required parameter redirectUrl is not supplied";
				logger.LogDebug($"Badrequest: {responseMessage}");
				return BadRequest(responseMessage);
			}

			logger.LogDebug($"Redirecting to identityservice with redirecturl '{redirectUrl}'");

			return Redirect(authConnector.GenerateLoginRedirectUrl(redirectUrl));
		}
	}
}
