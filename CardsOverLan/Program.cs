using CardsOverLan.Game;
using CardsOverLan.Game.Converters;
using CardsOverLan.Web;
using Nancy.Hosting.Self;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CardsOverLan
{
	class Program
	{
		static void Main(string[] args)
		{
			GameManager.Load();

			var mgr = GameManager.Instance;

			var hostCfg = new HostConfiguration
			{
				RewriteLocalhost = true,
				UrlReservations = new UrlReservations()
				{
					CreateAutomatically = true
				}
			};

			using (var host = new NancyHost(new Uri(mgr.Settings.Host), new GameBootstrapper(), hostCfg))
			using (var gameServer = new CardGameServer(mgr.Game))
			{
				// Start game server
				try
				{
					Console.Write("Starting WebSocket server... ");
					gameServer.Start();
					Console.WriteLine("Done.");
				}
				catch(Exception ex)
				{
					Console.WriteLine();
					Console.WriteLine($"Failed to start WebSocket server: \n{ex.Message}");
					return;
				}

				// Start webserver
				try
				{
					Console.Write("Starting webserver... ");
					host.Start();
					Console.WriteLine("Done.");
					Console.WriteLine($"Ready. Hosting on: {mgr.Settings.Host}");
				}
				catch(Exception ex)
				{
					Console.WriteLine();
					Console.WriteLine($"Failed to start webserver: \n{ex.Message}");
					return;
				}


				Console.ReadLine();
				Console.WriteLine("Stopping...");
			}
		}
	}
}
