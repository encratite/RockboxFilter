using System.Collections.Generic;

namespace RockboxFilter
{
	public class Configuration
	{
		public string TorrentDirectory = null;
		public List<int> Categories = new List<int>();
		public List<string> BannedGenres = new List<string>();
		public long? SizeLimit = null;
	}
}
