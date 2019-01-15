namespace Unosquare.Labs.EmbedIO.Samples
{
    using Modules;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using Swan;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    internal class Program
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        private static async Task Main(string[] args)
        {
            var url = args.Length > 0 ? args[0] : "http://*:8877";

            AppDbContext.InitDatabase();

            var ctSource = new CancellationTokenSource();
            ctSource.Token.Register(() => "Shutting down".Info());

            // Set a task waiting for press key to exit
#pragma warning disable 4014
            Task.Run(() =>
#pragma warning restore 4014
            {
                // Wait for any key to be pressed before disposing of our web server.
                Console.ReadLine();

                ctSource.Cancel();
            }, ctSource.Token);

            var webOptions = new WebServerOptions(url) { Mode = HttpListenerMode.EmbedIO };

            // Our web server is disposable. 
            using (var server = new WebServer(webOptions))
            {
                // First, we will configure our web server by adding Modules.
                // Please note that order DOES matter.
                // ================================================================================================
                // If we want to enable sessions, we simply register the LocalSessionModule
                // Beware that this is an in-memory session storage mechanism so, avoid storing very large objects.
                // You can use the server.GetSession() method to get the SessionInfo object and manipulate it.
                server.RegisterModule(new LocalSessionModule());

                // Set the CORS Rules
                server.RegisterModule(new CorsModule(
                    // Origins, separated by comma without last slash
                    "http://unosquare.github.io,http://run.plnkr.co",
                    // Allowed headers
                    "content-type, accept",
                    // Allowed methods
                    "post"));

                // Register the static files server. See the html folder of this project. Also notice that 
                // the files under the html folder have Copy To Output Folder = Copy if Newer
                server.RegisterModule(new StaticFilesModule(HtmlRootPath));

                // Register the Web Api Module. See the Setup method to find out how to do it
                // It registers the WebApiModule and registers the controller(s) -- that's all.
                server.WithWebApiController<PeopleController>();

                // Register the WebSockets module. See the Setup method to find out how to do it
                // It registers the WebSocketsModule and registers the server for the given paths(s)
                server.RegisterModule(new WebSocketsModule());
                server.Module<WebSocketsModule>().RegisterWebSocketsServer<WebSocketsChatServer>();
                server.Module<WebSocketsModule>().RegisterWebSocketsServer<WebSocketsTerminalServer>();

                server.RegisterModule(new FallbackModule((ctx, ct) => ctx.JsonResponseAsync(new { Message = "Error" }, ct)));

                // Fire up the browser to show the content!
                var browser = new Process
                {
                    StartInfo = new ProcessStartInfo(url.Replace("*", "localhost"))
                    {
                        UseShellExecute = true
                    }
                };

                browser.Start();

                // Once we've registered our modules and configured them, we call the RunAsync() method.
                if (!ctSource.IsCancellationRequested)
                    await server.RunAsync(ctSource.Token);

                // Clean up
                "Bye".Info();
                Terminal.Flush();
            }
        }

        /// <summary>
        /// Gets the HTML root path.
        /// </summary>
        /// <value>
        /// The HTML root path.
        /// </value>
        public static string HtmlRootPath
        {
            get
            {
                var assemblyPath = Path.GetDirectoryName(typeof(Program).GetTypeInfo().Assembly.Location);

                // This lets you edit the files without restarting the server.
#if DEBUG
                return Path.Combine(Directory.GetParent(assemblyPath).Parent.Parent.FullName, "html");
#else
                // This is when you have deployed the server.
                return Path.Combine(assemblyPath, "html");
#endif
            }
        }
    }
}