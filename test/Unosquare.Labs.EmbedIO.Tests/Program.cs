using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using Unosquare.Labs.EmbedIO.Modules;
using Unosquare.Labs.EmbedIO.Tests.TestObjects;
#if !NET46
using Unosquare.Net;
#endif
using Unosquare.Swan;

namespace Unosquare.Labs.EmbedIO.Tests
{
    public static class Program
    {
        public static void Main()
        {
            var url = Resources.GetServerAddress();
#if !NET46
            using (var instance = new WebServer(url))
            {
                instance.RegisterModule(new WebSocketsModule());
                instance.Module<WebSocketsModule>().RegisterWebSocketsServer(typeof(BigDataWebSocket));

                instance.RunAsync();

                var clientSocket = new WebSocket(url.Replace("http", "ws") + "bigdata");
                clientSocket.OnMessage += (s, e) =>
                {
                    e.Data.Info();
                };
                clientSocket.ConnectAsync().Wait();

                var buffer = System.Text.Encoding.UTF8.GetBytes("HOLA");
                clientSocket.SendAsync(buffer, Opcode.Text).Wait();
                Task.Delay(5000).Wait();
            }
#endif
            Console.ReadKey();
        }
    }
}