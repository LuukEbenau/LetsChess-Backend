using LetsChess_Backend.Logic;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LetsChess_Backend.Controllers
{
	[Route("api/auth")]
	[ApiController]
	public class AuthController : ControllerBase
	{
		private readonly AuthSettings authSettings;
		private readonly GoogleConnector authConnector;

		public AuthController(IOptions<AuthSettings> authSettings)
		{
			this.authSettings = authSettings.Value;
			authConnector = new GoogleConnector(authSettings.Value.ClientId);
		}
		[HttpGet("redirectToIdentity")]
		public async Task<IActionResult> RedirectToIdentity(string redirectUrl) {
			if (redirectUrl == default) return BadRequest("the Required parameter redirectUrl is not supplied");

			return Redirect(authConnector.GenerateLoginRedirectUrl(redirectUrl));
		}

		[HttpGet("userinfo")]
		public async Task<IActionResult> Userinfo()
		{
			if(Request.Headers.TryGetValue("authorization",out var authToken)){
				var accessToken = authToken.ToString().Split(' ').Last();
				var result = await authConnector.RetrieveUserInfo(accessToken);
				return Ok(result);
			}
			return Unauthorized();
		}

		[HttpPost("loginSuccessRedirect"), HttpGet("loginSuccessRedirect")]
		public IActionResult LoginSuccessRedirect() {		
			var uri = "http://localhost:3000/auth/redirected";
			return Redirect(uri);
		}
	}
}
