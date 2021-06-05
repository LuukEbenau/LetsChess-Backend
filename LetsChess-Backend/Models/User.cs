using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LetsChess_Backend.Models
{
	public class User
	{
		public string ExternalId { get; set; }
		public string Username { get; set; }
		public string ImageUrl { get; set; }
	}
}
