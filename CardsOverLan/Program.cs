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
                gameServer.Start();
                host.Start();
                Console.WriteLine($"Hosting on {mgr.Settings.Host}");
                Console.ReadLine();
                Console.WriteLine("Stopping...");
                gameServer.Stop();
            }
        }
    }
}
