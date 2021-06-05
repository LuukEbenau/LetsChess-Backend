using LetsChess_Backend.Models;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LetsChess_Backend.Logic
{
	public class UserServiceConnector
	{
		public string UserserviceEndpoint { get; }
		public UserServiceConnector(string userserviceEndpoint)
		{
			UserserviceEndpoint = userserviceEndpoint;
		}
		public async Task<object> Register(User user)
		{
			using HttpClient client = new();
			var data = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
			var url = $"{UserserviceEndpoint}/user/register";

			var result = await client.PostAsync(url,data);
			if (result.Content != null) {
				var response = await result.Content.ReadAsStringAsync();
				return response;
			}
			//TODO: what logic?
			return null;
		}
	}
}
