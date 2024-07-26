using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace NetBlox.Structs
{
	public class ServerStartupInfo
	{
		[JsonPropertyName("a")]
		public string PublicServiceAPI;
		[JsonPropertyName("b")]
		public string RbxlFilePath;
		[JsonPropertyName("c")]
		public string PlaceName;
		[JsonPropertyName("d")]
		public string PlaceAuthor;
		[JsonPropertyName("e")]
		public int PlaceVersion;
		[JsonPropertyName("f")]
		public ushort ServerPort;
	}
}
