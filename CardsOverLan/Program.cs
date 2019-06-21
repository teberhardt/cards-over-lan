using CardsOverLan.Analytics;
using CardsOverLan.Web;
using Nancy.Hosting.Self;
using System;
using System.Threading;

namespace CardsOverLan
{
    internal static class Program
    {
        private static void Main()
        {
            GameManager.Load();

            var mgr = GameManager.Instance;
            var mre = new ManualResetEventSlim(false);

            Console.TreatControlCAsInput = false;
            Console.CancelKeyPress += (sender, e) => mre.Set();

            var hostCfg = new HostConfiguration
            {
                RewriteLocalhost = true,
                EnableClientCertificates = true,	
                UrlReservations = new UrlReservations
                {
                    CreateAutomatically = true
                }
            };

            using (var host = new NancyHost(new Uri(mgr.Settings.HostUrl), new WebappBootstrapper(mgr.Settings.WebRoot), hostCfg))
            using (var gameServer = new CardGameServer(mgr.Game))
            {
                // Start analytics
                if (mgr.Settings.AnalyticsEnabled)
                {
                    AnalyticsManager.Instance.Start(mgr.Settings.AnalyticsPath);
                }

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

                // Start web server
                try
                {
                    Console.Write("Starting server... ");
                    host.Start();
                    Console.WriteLine("Done.");
                    Console.WriteLine($"Ready. Hosting on: {mgr.Settings.HostUrl}");
                }
                catch(Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Failed to start server: \n{ex.Message}");
                    return;
                }

                mre.Wait();

                // Close analytics
                AnalyticsManager.Instance.Stop();
            }
        }
    }
}