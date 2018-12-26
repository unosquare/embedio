namespace Unosquare.Labs.EmbedIO.Samples
{
    using Modules;
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
            var url = args.Length > 0 ? args[0] : "https://*:7876/";

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


            // Our web server is disposable. 
            using (var server = new WebServer(url))
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
                StaticFilesSample.Setup(server, useGzip: Runtime.IsUsingMonoRuntime == false);

                // Register the Web Api Module. See the Setup method to find out how to do it
                // It registers the WebApiModule and registers the controller(s) -- that's all.
                server.WithWebApiController<PeopleController>();

                // Register the WebSockets module. See the Setup method to find out how to do it
                // It registers the WebSocketsModule and registers the server for the given paths(s)
                WebSocketsSample.Setup(server);

                server.RegisterModule(new FallbackModule((ctx, ct) => ctx.JsonResponse(new {Message = "Error"})));

                // Fire up the browser to show the content!
                var browser = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo(url.Replace("*", "localhost"))
                    {
                        UseShellExecute = true
                    }
                };

                browser.Start();

                // Once we've registered our modules and configured them, we call the RunAsync() method.
                if (!ctSource.IsCancellationRequested)
                    await server.RunAsync(ctSource.Token);

                "Bye".Info();

                Terminal.Flush();
            }
        }
    }
}
