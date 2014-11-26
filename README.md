EmbedIO - Unosquare Labs
========================

A tiny, cross-platform, module based web server

* Cross-platform (tested in Mono 3.10.x)
* Extensible (Write your own modules like bearer token session handling, video streaming, upnp, etc.)
* Small memory footprint
* Create REST APIs quickly with the built-in Web Api Module
* Serve static files with 1 line of code (built-in module)
* Handle sessions with the built-in LocalSessionWebModule

*For detailed usage and REST API implementation, download the code and take a look at the Samples project*

Basic Example:
--------------

```csharp
namespace Company.Project
{
    using log4net;
    using System;
    using Unosquare.Labs.EmbedIO;

    class Program
    {
        private static readonly ILog Log = Logger.For<Program>();

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        static void Main(string[] args)
        {
            var url = "http://localhost:9696/";
            if (args.Length > 0)
                url = args[0];

            // Our web server is disposable. Note that if you don't want to use logging,
            // there are alternate constructors that allow you to skip specifying an ILog object.
            using (var server = new WebServer(url, Log))
            {
                // First, we will configure our web server by adding Modules.
                // Please note that order DOES matter.
                // ================================================================================================
                // If we want to enable sessions, we simply register the LocalSessionModule
                // Beware that this is an in-memory session storage mechanism so, avoid storing very large objects.
                // You can use the server.GetSession() method to get the SessionInfo object and manupulate it.
                // You could potentially implement a distributed session module using something like Redis
                server.RegisterModule(new Modules.LocalSessionModule());

                // Here we setup serving of static files
                server.RegisterModule(new Modules.StaticFilesModule("c:/inetpub/wwwroot"));
                // The static files module will cache small files in ram until it detects they have been modified.
                server.Module<Modules.StaticFilesModule>().UseRamCache = true;
                server.Module<Modules.StaticFilesModule>().DefaultExtension = ".html";
                // We don't need to add the line below. The default document is always index.html.
                //server.Module<Modules.StaticFilesWebModule>().DefaultDocument = "index.html";
                
                // Once we've registered our modules and configured them, we call the RunAsync() method.
                // This is a non-blocking method (it return immediately) so in this case we avoid
                // disposing of the object until a key is pressed.
                //server.Run();
                server.RunAsync();

                // Fire up the browser to show the content if we are debugging!
#if DEBUG
                var browser = new System.Diagnostics.Process() { 
                    StartInfo = new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true } };
                browser.Start();
#endif
                // Wait for any key to be pressed before disposing of our web server.
                // In a service we'd manage the lifecycle of of our web server using
                // something like a BackgroundWorker or a ManualResetEvent.
                Console.ReadKey(true);
            }

            // Before exiting, we shutdown the logging subsystem.
            Logger.Shutdown();
        }
    }
}
```

REST API Example:
-----------------

*During server setup:*
```csharp
    server.RegisterModule(new WebApiModule());
    server.Module<WebApiModule>().RegisterController<PeopleController>();
```

*And our controller class looks like:*

```csharp
        public class PeopleController : WebApiController
        {
            [WebApiHandler(HttpVerbs.Get, "/api/people/*")]
            public bool GetPeople(WebServer server, HttpListenerContext context)
            {
                try
                {
                    var lastSegment = context.Request.Url.Segments.Last();
                    if (lastSegment.EndsWith("/"))
                        return context.JsonResponse(People);

                    int key = 0;
                    if (int.TryParse(lastSegment, out key) && People.Any(p => p.Key == key))
                    {
                        return context.JsonResponse(People.FirstOrDefault(p => p.Key == key));
                    }

                    throw new KeyNotFoundException("Key Not Found: " + lastSegment);
                }
                catch (Exception ex)
                {
                    return HandleError(context, ex, (int)HttpStatusCode.InternalServerError);
                }
            }
            
            protected bool HandleError(HttpListenerContext context, Exception ex, int statusCode = 500)
            {
                var errorResponse = new
                {
                    Title = "Unexpected Error",
                    ErrorCode = ex.GetType().Name,
                    Description = ex.ExceptionMessage(),
                };

                context.Response.StatusCode = statusCode;
                return context.JsonResponse(errorResponse);
            }
        }
```

