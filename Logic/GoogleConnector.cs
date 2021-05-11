using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.WebUtilities;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace LetsChess_Backend.Logic
{
	public class GoogleConnector
	{
		private readonly string clientId;

		public GoogleConnector(string clientId)
		{
			this.clientId = clientId;
		}

		public async Task<string> RetrieveUserInfo(string accessToken) {
			using HttpClient client = new();
			
			client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, accessToken);
			var result = await client.GetStringAsync("https://openidconnect.googleapis.com/v1/userinfo");
			return result;
		}

		public string GenerateLoginRedirectUrl(string postLoginRedirectUrl) {
			string url = "https://accounts.google.com/o/oauth2/v2/auth";

			Dictionary<string, string> param = new()
			{
				{ "client_id", clientId },
				{ "redirect_uri", postLoginRedirectUrl },
				{ "response_type", "token" },
				{ "scope", "profile" },
			};

			var newUrl = new Uri(QueryHelpers.AddQueryString(url, param));
			return newUrl.ToString();
		}
	}
}
