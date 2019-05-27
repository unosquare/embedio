using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Actions;
using EmbedIO.WebApi;
using Unosquare.Swan;

namespace EmbedIO.Samples
{
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

            using (var ctSource = new CancellationTokenSource())
            {
                ctSource.Token.Register(() => "Shutting down".Info(nameof(Main)));

                // Set a task waiting for press key to exit
#pragma warning disable 4014
                Task.Run(() =>
#pragma warning restore 4014
                {
                    // Wait for any key to be pressed before disposing of our web server.
                    Console.ReadLine();

                    ctSource.Cancel();
                }, ctSource.Token);

                // Our web server is disposable. 
                using (var server = CreateWebServer(url))
                {
                    // Fire up the browser to show the content!
                    var browser = new Process
                    {
                        StartInfo = new ProcessStartInfo(url.Replace("*", "localhost"))
                        {
                            UseShellExecute = true
                        }
                    };

                    browser.Start();

                    // Call the RunAsync() method to fire up the web server.
                    if (!ctSource.IsCancellationRequested)
                        await server.RunAsync(ctSource.Token).ConfigureAwait(false);

                    // Clean up
                    "Bye".Info(nameof(Program));
                    Terminal.Flush();
                }
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
                var assemblyPath = Path.GetDirectoryName(typeof(Program).Assembly.Location);

                // This lets you edit the files without restarting the server.
#if DEBUG
                return Path.Combine(Directory.GetParent(assemblyPath).Parent.Parent.FullName, "html");
#else
                // This is when you have deployed the server.
                return Path.Combine(assemblyPath, "html");
#endif
            }
        }

        private static WebServer CreateWebServer(string url)
        {
            var options = new WebServerOptions(url)
            {
                Mode = HttpListenerMode.EmbedIO
            };

            var server = new WebServer(options)
                .WithLocalSessionManager()
                .WithCors(
                    // Origins, separated by comma without last slash
                    "http://unosquare.github.io,http://run.plnkr.co",
                    // Allowed headers
                    "content-type, accept",
                    // Allowed methods
                    "post")
                .WithWebApi("/api", m => m
                    .WithController<PeopleController>())
                .WithModule(new WebSocketChatModule("/chat"))
                .WithModule(new WebSocketTerminalModule("/terminal"))
                .WithStaticFolderAt("/", HtmlRootPath) // Add static files after other modules to avoid conflicts
                .WithModule(new ActionModule("/", HttpVerbs.Any, (ctx, path, ct) => ctx.JsonResponseAsync(new { Message = "Error" }, ct)));

            // Listen for state changes.
            server.StateChanged += (s, e) => $"WebServer New State - {e.NewState}".Info();

            return server;
        }
    }
}