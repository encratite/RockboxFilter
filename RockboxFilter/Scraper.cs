using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Logger = Ashod.Logger;

namespace RockboxFilter
{
	class Scraper
	{
		private const string _Site = "http://psychocydd.co.uk/";

		private Configuration _Configuration;

		public Scraper(Configuration configuration)
		{
			_Configuration = configuration;
		}

		public void Run()
		{
			var directory = _Configuration.TorrentDirectory;
			if (!Directory.Exists(directory))
			{
				Logger.Warning("Creating torrent directory {0}", directory);
				Directory.CreateDirectory(directory);
			}
			foreach (var category in _Configuration.Categories)
				ProcessCategory(category);
		}

		private void ProcessCategory(int category)
		{
			Logger.Log("Processing category {0}", category);
			for(int page = 1; ; page++)
			{
				try
				{
					if (!ProcessPage(category, page))
						break;
				}
				catch (Exception exception)
				{
					Logger.Exception("Failed to process page {0}", exception, page);
				}
			}
		}

		private bool ProcessPage(int category, int page)
		{
			Logger.Log("Processing page {0}", page);
			string uri = string.Format("{0}torrents.php?active=1&category={1}&options=0&order=data&by=desc&page={2}", _Site, category, page);
			string content = Download(uri);
			if (content.IndexOf("No torrents here...") >= 0)
			{
				Logger.Log("No more torrents available in this category");
				return false;
			}
			var document = new HtmlDocument();
			document.LoadHtml(content);
			var table = document.DocumentNode.SelectSingleNode("//table[@width = '100%' and @class = 'lista' and .//a[contains(@href, 'download.php')]][last()]");
			if (table == null)
				throw new ApplicationException("Unable to find table containing torrents");
			var tasks = new List<Task>();
			foreach (var node in table.SelectNodes(".//tr[position() >= 3]"))
			{
				const string prefix = "View history: ";
				var nameNode = node.SelectSingleNode(string.Format(".//a[contains(@title, '{0}')]", prefix));
				if (nameNode == null)
					throw new ApplicationException("Unable to find name node");
				string name = nameNode.Attributes["title"].Value.Substring(prefix.Length);
				string torrentName = name.Replace("[Request] ", "").Replace("..", "_").Replace("\\", "_").Replace("/", "_") + ".torrent";
				string torrentPath = Path.Combine(_Configuration.TorrentDirectory, torrentName);
				var downloadNode = node.SelectSingleNode(".//a[contains(@href, 'download.php')]");
				if (downloadNode == null)
					throw new ApplicationException("Unable to find download link");
				string link = _Site + downloadNode.Attributes["href"].Value;
				var genreNode = node.SelectSingleNode(".//span");
				if (genreNode == null)
					throw new ApplicationException("Unable to find genre node");
				string genre = genreNode.InnerText;
				var sizeNode = node.SelectSingleNode(".//td[@class = 'lista' and (contains(text(), ' MB') or contains(text(), ' GB'))]");
				if (sizeNode == null)
					throw new ApplicationException("Unable to find size node");
				double size = Ashod.FileSize.FromString(sizeNode.InnerText);
				if (IsBannedGenre(genre))
				{
					Logger.Warning("Banned genre \"{1}\" in release \"{0}\"", name, genre);
					continue;
				}
				if (size > _Configuration.SizeLimit)
				{
					Logger.Warning("Release \"{0}\" exceeds the filesize limit", name);
					continue;
				}
				if (File.Exists(torrentPath))
				{
					Logger.Log("\"{0}\" has already been downloaded", name);
					continue;
				}
				var task = new Task(() => DownloadTorrent(name, link, torrentPath));
				task.Start();
				tasks.Add(task);
			}
			foreach (var task in tasks)
				task.Wait();
			return true;
		}

		private void DownloadTorrent(string name, string link, string torrentPath)
		{
			try
			{
				Logger.Log("Downloading \"{0}\"", name);
				var client = new WebClient();
				client.DownloadFile(link, torrentPath);
			}
			catch (Exception exception)
			{
				Logger.Exception("Failed to download \"{0}\"", exception, name);
			}
		}

		private bool IsBannedGenre(string genre)
		{
			foreach (var bannedGenre in _Configuration.BannedGenres)
			{
				if (genre.IndexOf(bannedGenre) >= 0)
					return true;
			}
			return false;
		}

		private string Download(string uri)
		{
			var client = new WebClient();
			string output = client.DownloadString(new Uri(uri));
			return output;
		}
	}
}
