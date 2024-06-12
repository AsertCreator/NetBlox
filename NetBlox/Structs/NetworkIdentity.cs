using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Xml;

namespace NetBlox.Structs
{
	public class NetworkIdentity
	{
		public string PlaceName = string.Empty;
		public string UniverseName = string.Empty;
		public string Author = string.Empty;
		public ulong PlaceID;
		public ulong UniverseID;
		public uint MaxPlayerCount;
		public uint UniquePlayerID;

		public void Reset()
		{
			PlaceName = string.Empty;
			UniverseName = string.Empty;
			Author = string.Empty;
			PlaceID = 0;
			UniverseID = 0;
			MaxPlayerCount = 0;
		}
	}
}
