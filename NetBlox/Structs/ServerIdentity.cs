using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace NetBlox.Structs
{
	public class ServerIdentity
	{
		public string PlaceName = string.Empty;
		public string UniverseName = string.Empty;
		public string Author = string.Empty;
		public ulong PlaceID;
		public ulong UniverseID;
		public uint MaxPlayerCount;
		public XmlDocument PlaceXMLData;
	}
}
