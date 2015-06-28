 [![Analytics](https://ga-beacon.appspot.com/UA-8535255-2/unosquare/embedio/)](https://github.com/igrigorik/ga-beacon)
 [![Build Status](https://travis-ci.org/unosquare/embedio.svg?branch=master)](https://travis-ci.org/unosquare/embedio)
 [![Build status](https://ci.appveyor.com/api/projects/status/w59t7sct3a8ir96t?svg=true)](https://ci.appveyor.com/project/geoperez/embedio)
 [![NuGet version](https://badge.fury.io/nu/embedio.svg)](https://www.nuget.org/packages/Embedio)
[![Coverage Status](https://coveralls.io/repos/unosquare/embedio/badge.svg?branch=master)](https://coveralls.io/r/unosquare/embedio?branch=master)

![EmbedIO](http://unosquare.github.io/embedio/images/embedio.png)

A tiny, cross-platform, module based, MIT-licensed web server.

* Network operations use the relatively recent async/await pattern
* Cross-platform (tested in Mono 3.10.x on Windows and on a custom Yocto image for the Raspberry Pi)
* Extensible (Write your own modules. For example, video streaming, UPnP, etc.). Check <a href="https://github.com/unosquare/embedio-extras" target="_blank">EmbedIO Extras</a> for more modules.
* Small memory footprint
* Create REST APIs quickly with the out-of-the-box Web Api module
* Serve static files with 1 line of code (also out-of-the-box)
* Handle sessions with the built-in LocalSessionWebModule
* Web Sockets support (Not available on Mono though)
* CORS support. Origin, Headers and Methods validation with OPTIONS preflight
* [OWIN](http://owin.org/) Middleware support via [Owin Middleware Module](https://github.com/unosquare/embedio-extras/tree/master/Unosquare.Labs.EmbedIO.OwinMiddleware).

*For detailed usage and REST API implementation, download the code and take a look at the Samples project*

NuGet Installation:
-------------------
```
PM> Install-Package EmbedIO
```

Basic Example:
--------------

*Please note the comments are the important part here. More info is available in the samples.*

```csharp
namespace Company.Project
{
    using System;
    using Unosquare.Labs.EmbedIO;
    using Unosquare.Labs.EmbedIO.Log;
    using Unosquare.Labs.EmbedIO.Modules;

    class Program
    {
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
            using (var server = new WebServer(url, new SimpleConsoleLog()))
            {
                // First, we will configure our web server by adding Modules.
                // Please note that order DOES matter.
                // ================================================================================================
                // If we want to enable sessions, we simply register the LocalSessionModule
                // Beware that this is an in-memory session storage mechanism so, avoid storing very large objects.
                // You can use the server.GetSession() method to get the SessionInfo object and manupulate it.
                // You could potentially implement a distributed session module using something like Redis
                server.RegisterModule(new LocalSessionModule());

                // Here we setup serving of static files
                server.RegisterModule(new StaticFilesModule("c:/web"));
                // The static files module will cache small files in ram until it detects they have been modified.
                server.Module<StaticFilesModule>().UseRamCache = true;
                server.Module<StaticFilesModule>().DefaultExtension = ".html";
                // We don't need to add the line below. The default document is always index.html.
                //server.Module<Modules.StaticFilesWebModule>().DefaultDocument = "index.html";

                // Once we've registered our modules and configured them, we call the RunAsync() method.
                // This is a non-blocking method (it return immediately) so in this case we avoid
                // disposing of the object until a key is pressed.
                //server.Run();
                server.RunAsync();

                // Fire up the browser to show the content if we are debugging!
#if DEBUG
                var browser = new System.Diagnostics.Process()
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }
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
```

Fluent Example:
---------------

```csharp
namespace Company.Project
{
    using System;
    using Unosquare.Labs.EmbedIO;

    internal class Program
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        private static void Main(string[] args)
        {
            var url = "http://localhost:9696/";
            if (args.Length > 0)
                url = args[0];

            // Create Webserver with console logger and attach LocalSession and Static
            // files module and CORS enabled
            var server = WebServer
                .CreateWithConsole(url)
                .EnableCors()
                .WithLocalSession()
                .WithStaticFolderAt("c:/web");

            server.RunAsync();

            // Fire up the browser to show the content if we are debugging!
#if DEBUG
            var browser = new System.Diagnostics.Process()
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo(url) {UseShellExecute = true}
            };
            browser.Start();
#endif
            // Wait for any key to be pressed before disposing of our web server.
            // In a service we'd manage the lifecycle of of our web server using
            // something like a BackgroundWorker or a ManualResetEvent.
            Console.ReadKey(true);

            server.Dispose();
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

Web Sockets Example:
-----------------

*During server setup:*
```csharp
server.RegisterModule(new WebSocketsModule());
server.Module<WebSocketsModule>().RegisterWebSocketsServer<WebSocketsChatServer>("/chat");
```

*And our web sockets server class looks like:*

```csharp

/// <summary>
/// Defines a very simple chat server
/// </summary>
public class WebSocketsChatServer : WebSocketsServer
{

    public WebSocketsChatServer()
        : base(true, 0)
    {
        // placeholder
    }

    /// <summary>
    /// Called when this WebSockets Server receives a full message (EndOfMessage) form a WebSockets client.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="rxBuffer">The rx buffer.</param>
    /// <param name="rxResult">The rx result.</param>
    protected override void OnMessageReceived(WebSocketContext context, byte[] rxBuffer, WebSocketReceiveResult rxResult)
    {
        var session = this.WebServer.GetSession(context);
        foreach (var ws in this.WebSockets)
        {
            if (ws != context)
                this.Send(ws, Encoding.UTF8.GetString(rxBuffer));
        }
    }

    /// <summary>
    /// Gets the name of the server.
    /// </summary>
    /// <value>
    /// The name of the server.
    /// </value>
    public override string ServerName
    {
        get { return "Chat Server"; }
    }

    /// <summary>
    /// Called when this WebSockets Server accepts a new WebSockets client.
    /// </summary>
    /// <param name="context">The context.</param>
    protected override void OnClientConnected(WebSocketContext context)
    {
        this.Send(context, "Welcome to the chat room!");
        foreach (var ws in this.WebSockets)
        {
            if (ws != context)
                this.Send(ws, "Someone joined the chat room.");
        }
    }

    /// <summary>
    /// Called when this WebSockets Server receives a message frame regardless if the frame represents the EndOfMessage.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="rxBuffer">The rx buffer.</param>
    /// <param name="rxResult">The rx result.</param>
    protected override void OnFrameReceived(WebSocketContext context, byte[] rxBuffer, WebSocketReceiveResult rxResult)
    {
        return;
    }

    /// <summary>
    /// Called when the server has removed a WebSockets connected client for any reason.
    /// </summary>
    /// <param name="context">The context.</param>
    protected override void OnClientDisconnected(WebSocketContext context)
    {
        this.Broadcast(string.Format("Someone left the chat room."));
    }
}
```
