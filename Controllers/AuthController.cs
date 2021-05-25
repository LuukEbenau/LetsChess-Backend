using LetsChess_Backend.Logic;
using LetsChess_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using LetsChess_Backend.WSClients;

namespace LetsChess_Backend.Controllers
{
	[Route("auth")]
	[ApiController]
	public class AuthController : ControllerBase
	{
		private readonly AuthSettings authSettings;
		private readonly GoogleConnector authConnector;
		private readonly UserServiceConnector userServiceConnector;
		private readonly ILogger<AuthController> logger;

		public AuthController(IOptions<AuthSettings> authSettings, IOptions<ServiceEndpoints> serviceEndpoints, ILogger<AuthController> logger)
		{
			this.authSettings = authSettings.Value;
			authConnector = new GoogleConnector(authSettings.Value.ClientId);
			userServiceConnector = new UserServiceConnector(serviceEndpoints?.Value?.UserService);

			this.logger = logger;
		}

		[HttpGet("redirectToIdentity")]
		public IActionResult RedirectToIdentity(string redirectUrl) {
			if (redirectUrl == default) {
				var responseMessage = "the Required parameter redirectUrl is not supplied";
				logger.LogDebug($"Badrequest: {responseMessage}");
				return BadRequest(responseMessage); 
			}

			logger.LogDebug($"Redirecting to identityservice with redirecturl '{redirectUrl}'");

			return Redirect(authConnector.GenerateLoginRedirectUrl(redirectUrl));
		}

		[HttpGet("userinfo")]
		public async Task<IActionResult> Userinfo()
		{
			logger.LogDebug($"Requesting userinfo");

			if (Request.Headers.TryGetValue("authorization",out var authToken)){
				var accessToken = authToken.ToString().Split(' ').Last();
				var result = await authConnector.RetrieveUserInfo(accessToken);
				try
				{
					// send it to the userservice
					var userData = JsonConvert.DeserializeObject<GoogleUserInfoResult>(result);
					var userinfoResult = await userServiceConnector.Register(new User { ExternalId = userData.Sub, ImageUrl = userData.Picture, Username = userData.Name });

					return Ok(JsonConvert.SerializeObject(userinfoResult));
				}
				catch (Exception e) {
					logger.LogError(e, $"error from userservice: {e.Message}");
					throw new Exception("Connection to userservice could not be established",e);
				}
			}

			return Unauthorized();
		}
	}
}
