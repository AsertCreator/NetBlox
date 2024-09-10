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
		public string WindowName = "NetBlox";
		[JsonPropertyName("c")]
		public string Username = "";
		[JsonPropertyName("d")]
		public string PasswordHash = "";
		[JsonPropertyName("e")]
		public bool IsGuest = true;
		[JsonPropertyName("f")]
		public bool IsTouchDevice = false;
		[JsonPropertyName("g")]
		public string ServerIP;
		[JsonPropertyName("h")]
		public string ServerPort;
	}
}
