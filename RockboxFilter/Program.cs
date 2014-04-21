using Ashod;

namespace RockboxFilter
{
	class Program
	{
		static void Main(string[] args)
		{
			var configuration = XmlFile.Read<Configuration>();
			var scraper = new Scraper(configuration);
			scraper.Run();
		}
	}
}
