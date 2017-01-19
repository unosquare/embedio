using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO.Modules;
using Unosquare.Labs.EmbedIO.Tests.TestObjects;
using Unosquare.Swan;

namespace Unosquare.Labs.EmbedIO.Tests
{
    public static class Program
    {
        public static void Main()
        {
            var webServerUrl = Resources.GetServerAddress();

            var webServer = new WebServer(webServerUrl);
            webServer.RegisterModule(new LocalSessionModule() { Expiration = TimeSpan.FromSeconds(6) });
            webServer.RegisterModule(new FallbackModule((ws, ctx) => ctx.JsonResponse(new { Message = "OK" })));

            webServer.RunAsync();

            var rnd = new Random();

            Parallel.ForEach(Enumerable.Range(0, 50), async (s, t, l) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(rnd.Next(1, 10)));

                using (var webClient = new HttpClient())
                {
                    var data = await webClient.GetStringAsync(webServerUrl);
                    data.Info();
                }
            });

            Console.ReadKey();
        }
    }
}