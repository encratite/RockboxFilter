namespace RockboxFilter
{
	class Program
	{
		static void Main(string[] args)
		{
			var configuration = Ashod.Configuration.Read<Configuration>();
			var scraper = new Scraper(configuration);
			scraper.Run();
		}
	}
}
