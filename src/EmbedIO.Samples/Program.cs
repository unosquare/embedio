﻿using System;
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
        private static void Main(string[] args)
        {
            var url = args.Length > 0 ? args[0] : "http://*:8877";

            AppDbContext.InitDatabase();

            using (var ctSource = new CancellationTokenSource())
            {
                Task.WaitAll(
                    RunWebServerAsync(url, ctSource.Token),
                    ShowBrowserAsync(url.Replace("*", "localhost"), ctSource.Token),
                    WaitForUserBreakAsync(ctSource.Cancel));
            }

            // Clean up
            "Bye".Info(nameof(Program));
            Terminal.Flush();

            Console.WriteLine("Press any key to exit.");
            WaitForKeypress();
        }

        // Gets the local path of shared files.
        // When debugging, take them directly from source so we can edit and reload.
        // Otherwise, take them from the deployment directory.
        public static string HtmlRootPath
        {
            get
            {
                var assemblyPath = Path.GetDirectoryName(typeof(Program).Assembly.Location);

#if DEBUG
                return Path.Combine(Directory.GetParent(assemblyPath).Parent.Parent.FullName, "html");
#else
                return Path.Combine(assemblyPath, "html");
#endif
            }
        }

        // Create and configure our web server.
        private static WebServer CreateWebServer(string url)
        {
            var options = new WebServerOptions(url) {
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

        // Create and run a web server.
        private static async Task RunWebServerAsync(string url, CancellationToken cancellationToken)
        {
            using (var server = CreateWebServer(url))
            {
                await server.RunAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        // Open the default browser on the web server's home page.
        private static async Task ShowBrowserAsync(string url, CancellationToken cancellationToken)
        {
            // Be sure to run in parallel.
            await Task.Yield();

            // Fire up the browser to show the content!
            new Process {
                StartInfo = new ProcessStartInfo(url) {
                    UseShellExecute = true
                }
            }.Start();
        }

        // Prompt the user to press any key; when a key is next pressed,
        // call the specified action to cancel operations.
        private static async Task WaitForUserBreakAsync(Action cancel)
        {
            // Be sure to run in parallel.
            await Task.Yield();

            "Press any key to stop the web server.".Info(nameof(Program));
            WaitForKeypress();
            "Stopping...".Info(nameof(Program));
            cancel();
        }

        // Clear the console input buffer and wait for a keypress
        private static void WaitForKeypress()
        {
            while (Console.KeyAvailable)
                Console.ReadKey(true);

            Console.ReadKey(true);
        }
    }
}