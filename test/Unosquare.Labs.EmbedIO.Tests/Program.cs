using System;
using System.IO;
using System.Linq;
using System.Net;

#if NET47
using System.Net.WebSockets;
#else
using Unosquare.Net;
#endif
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Unosquare.Labs.EmbedIO.Modules;
using Unosquare.Labs.EmbedIO.Tests.TestObjects;
using Unosquare.Swan;
using Unosquare.Swan.Formatters;

namespace Unosquare.Labs.EmbedIO.Tests
{
    public static class Program
    {
        public static void Main()
        {
            var ct = new CancellationTokenSource();

            Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        const string wsUrl = Resources.WsServerAddress + "test";

                        using (var instance = new WebServer(Resources.WsServerAddress.Replace("ws", "http")))
                        {
                            instance.RegisterModule(new WebSocketsModule());
                            instance.Module<WebSocketsModule>().RegisterWebSocketsServer<TestWebSocket>();
                            instance.Module<WebSocketsModule>().RegisterWebSocketsServer<BigDataWebSocket>();

                            var runTask = instance.RunAsync();
#if NET47
                            var clientSocket = new ClientWebSocket();
                            await clientSocket.ConnectAsync(new Uri(wsUrl), ct.Token);
                            
                            var message = new ArraySegment<byte>(System.Text.Encoding.Default.GetBytes("HOLA"));
                            var buffer = new ArraySegment<byte>(new byte[5]);

                            await clientSocket.SendAsync(message, System.Net.WebSockets.WebSocketMessageType.Text, true,
                                ct.Token);
                            await clientSocket.ReceiveAsync(buffer, ct.Token);
                            System.Text.Encoding.UTF8.GetString(buffer.Array).Trim().Info();

                            await clientSocket.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, ct.Token);
#else
                            var clientSocket = new WebSocket(wsUrl);
                            await clientSocket.ConnectAsync(ct.Token);
                            clientSocket.OnMessage += (s, e) =>
                            {
                                e.Data.Info();
                            };
                            
                            var buffer = System.Text.Encoding.UTF8.GetBytes("HOLA");
                            await clientSocket.SendAsync(buffer, Opcode.Text, ct.Token);
                            await Task.Delay(500, ct.Token);
#endif
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.Log(nameof(Main));
                    }
                });
            Console.ReadKey();
        }
    }
}