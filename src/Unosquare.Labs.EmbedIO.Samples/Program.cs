namespace Unosquare.Labs.EmbedIO.Samples
{
    using Swan;
    using Modules;
    using System;

    internal class Program
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        private static void Main(string[] args)
        {
            $"Running on Mono Runtime: {Runtime.IsUsingMonoRuntime}".Info();

            var url = "http://localhost:8787/";

            if (args.Length > 0)
                url = args[0];

#if !MONO
            var dbContext = new AppDbContext();

            foreach (var person in dbContext.People.SelectAll())
                dbContext.People.Delete(person);

            dbContext.People.Insert(new Person()
            {
                Name = "Mario Di Vece",
                Age = 31,
                EmailAddress = "mario@unosquare.com"
            });
            dbContext.People.Insert(new Person()
            {
                Name = "Geovanni Perez",
                Age = 32,
                EmailAddress = "geovanni.perez@unosquare.com"
            });

            dbContext.People.Insert(new Person()
            {
                Name = "Luis Gonzalez",
                Age = 29,
                EmailAddress = "luis.gonzalez@unosquare.com"
            });
#endif

            // Our web server is disposable. 
            using (var server = new WebServer(url))
            {
                // First, we will configure our web server by adding Modules.
                // Please note that order DOES matter.
                // ================================================================================================
                // If we want to enable sessions, we simply register the LocalSessionModule
                // Beware that this is an in-memory session storage mechanism so, avoid storing very large objects.
                // You can use the server.GetSession() method to get the SessionInfo object and manupulate it.
                server.RegisterModule(new LocalSessionModule());

                // Set the CORS Rules
                server.RegisterModule(new CorsModule(
                    // Origins, separated by comma without last slash
                    "http://client.cors-api.appspot.com,http://unosquare.github.io,http://run.plnkr.co",
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

                server.RegisterModule(new FallbackModule((ws, ctx) =>
                {
                    ctx.JsonResponse(new {Message = "Error "});
                    return true;
                }));

                // Once we've registered our modules and configured them, we call the RunAsync() method.
                server.RunAsync();

                // Fire up the browser to show the content!
#if DEBUG && !NETCOREAPP1_1
                var browser = new System.Diagnostics.Process()
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo(url)
                    {
                        UseShellExecute = true
                    }
                };
                browser.Start();
#endif
                // Wait for any key to be pressed before disposing of our web server.
                // In a service we'd manage the lifecycle of of our web server using
                // something like a BackgroundWorker or a ManualResetEvent.
                Console.ReadKey(true);
            }
        }
    }
}