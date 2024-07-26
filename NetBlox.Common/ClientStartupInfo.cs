using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace NetBlox.Structs
{
	public class ClientStartupInfo
	{
		[JsonPropertyName("a")]
		public string PublicServiceAPI;
		[JsonPropertyName("b")]
		public string ForcedUsername;
		[JsonPropertyName("c")]
		public int UserId;
		[JsonPropertyName("d")]
		public string LoginToken;
		[JsonPropertyName("e")]
		public bool IsGuest;
		[JsonPropertyName("f")]
		public bool IsTouchDevice;
		[JsonPropertyName("g")]
		public string ServerIP;
		[JsonPropertyName("h")]
		public string ServerPort;
	}
}
