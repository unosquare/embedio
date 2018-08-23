 [![Analytics](https://ga-beacon.appspot.com/UA-8535255-2/unosquare/embedio/)](https://github.com/igrigorik/ga-beacon)
 [![Build status](https://ci.appveyor.com/api/projects/status/w59t7sct3a8ir96t?svg=true)](https://ci.appveyor.com/project/geoperez/embedio)
 [![Build Status](https://travis-ci.org/unosquare/embedio.svg?branch=master)](https://travis-ci.org/unosquare/embedio)
 [![NuGet version](https://badge.fury.io/nu/embedio.svg)](https://www.nuget.org/packages/Embedio)
 [![NuGet](https://img.shields.io/nuget/dt/embedio.svg)](https://www.nuget.org/packages/Embedio)
[![Coverage Status](https://coveralls.io/repos/unosquare/embedio/badge.svg?branch=master)](https://coveralls.io/r/unosquare/embedio?branch=master)

![EmbedIO](http://unosquare.github.io/embedio/images/embedio.png)

*:star: Please star this project if you find it useful!*


- [Overview](#overview)
    - [Some usage scenarios](#some-usage-scenarios)
- [NuGet Installation](#nuget-installation)
- [Examples](#examples)
    - [Basic Example](#basic-example)
    - [Fluent Example](#fluent-example)
    - [REST API Example](#rest-api-example)
    - [WebSockets Example](#websockets-example)
- [Related Projects and Nugets](#related-projects-and-nugets)
- [Notes](#notes)


## Overview
A tiny, cross-platform, module based, MIT-licensed web server for .NET Framework and .NET Core.

* Written entirely in C#, using our helpful library [SWAN](https://github.com/unosquare/swan)
* Network operations use the async/await pattern: Responses are handled asynchronously
* Cross-platform[1]: tested in Mono on Windows and on a custom Yocto image for the Raspberry Pi
* Extensible: Write your own modules -- For example, video streaming, UPnP, etc. Check out <a href="https://github.com/unosquare/embedio-extras" target="_blank">EmbedIO Extras</a> for additional modules.
* Small memory footprint
* Create REST APIs quickly with the out-of-the-box Web API module
* Serve static files with 1 line of code (also out-of-the-box)
* Handle sessions with the built-in LocalSessionWebModule
* WebSockets support (see notes below)
* CORS support. Origin, Header and Method validation with OPTIONS preflight
* Supports HTTP 206 Partial Content

*For detailed usage and REST API implementation, download the code and take a look at the Samples project*

### **Some usage scenarios**:

* Write a cross-platform GUI entirely in CSS/HTML/JS
* Write a game using Babylon.js and make EmbedIO your serve your code and assets
* Create GUIs for Windows services or Linux daemons
* Works well with <a href="https://github.com/unosquare/litelib" target="_blank">LiteLib</a> - add SQLite support in minutes!
* Write client applications with real-time communication between them

Some notes regarding WebSocket and runtimes support:

| Runtime | WebSocket support | Notes |
| --- | --- | --- |
| NET46 | Yes | Support Win7+ OS using a custom System.Net implementation based on Mono and [websocket-sharp](https://github.com/sta/websocket-sharp/) |
| NET47 | Yes | Support Win8+ OS using native System.Net library |
| NETSTANDARD* | Yes | Support Windows, Linux and macOS using native System.Net library |
| UAP | No | Support Windows Universal Platform. More information [here](https://github.com/unosquare/embedio/tree/master/src/Unosquare.Labs.EmbedIO.IoT) |

EmbedIO before version 1.4.0 uses Newtonsoft JSON and an internal logger subsystem based on ILog interface.

## NuGet Installation:
```
PM> Install-Package EmbedIO
```

## Examples

## Basic Example:

*Please note the comments are the important part here. More info is available in the samples.*

```csharp
namespace Company.Project
{
    using System;
    using Unosquare.Labs.EmbedIO;
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

            // Our web server is disposable.
            using (var server = new WebServer(url))
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
                // In a service, we'd manage the lifecycle of our web server using
                // something like a BackgroundWorker or a ManualResetEvent.
                Console.ReadKey(true);
            }
        }
    }
}
```

## Fluent Example:

Many extension methods are available. This allows you to create a web server instance in a fluent style by dotting in configuration options.

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

            // Create Webserver and attach LocalSession and Static
            // files module and CORS enabled
            var server = WebServer
                .Create(url)
                .EnableCors()
                .WithLocalSession()
                .WithStaticFolderAt("c:/web");

            var cts = new CancellationTokenSource();
            var task = server.RunAsync(cts.Token);

            Console.ReadKey(true);
            cts.Cancel();

			// Wait before dispose server
            task.Wait();
            server.Dispose();
        }
    }
}
```

## REST API Example:

The WebApi module supports two routing strategies: Wildcard and Regex. By default, and in order to maintain backward compatibility, the WebApi module will use the **Wildcard Routing Strategy** and match routes using the asterisk `*` character in the route. **For example:** 
- The route `/api/people/*` will match any request with a URL starting with the two first URL segments `api` and `people` and ending with anything. The route `/api/people/hello` will be matched.
- You can also use wildcards in the middle of the route. The route `/api/people/*/details` will match requests starting with the two first URL segments `api` and `people`, and end with a `details` segment. The route `/api/people/hello/details` will be matched. 

*Note that most REST services can be designed with this simpler Wildcard routing strategy. However, the Regex matching strategy is the current recommended approach as we might be deprecating the Wildcard strategy altogether*

On the other hand, the **Regex Routing Strategy** will try to match and resolve the values from a route template, in a similar fashion to Microsoft's Web API 2. A method with the following route `/api/people/{id}` is going to match any request URL with three segments: the first two `api` and `people` and the last one is going to be parsed or converted to the type in the `id` argument of the handling method signature. Please read on if this was confusing as it is much simpler than it sounds. Additionally, you can put multiple values to match, for example `/api/people/{mainSkill}/{age}`, and receive the parsed values from the URL straight into the arguments of your handler method.

*During server setup:*

```csharp
// The routing strategy is Wildcard by default, but you can change it to Regex as follows:
var server =  new WebServer("http://localhost:9696/", RoutingStrategy.Regex);

server.RegisterModule(new WebApiModule());
server.Module<WebApiModule>().RegisterController<PeopleController>();
```

*And our controller class (using Regex Strategy) looks like:*

```csharp
public class PeopleController : WebApiController
{
    public PeopleController(IHttpContext context)
    : base(context)
    {
    }

    [WebApiHandler(HttpVerbs.Get, "/api/people/{id}")]
    public bool GetPeople(int id)
    {
        try
        {
            if (People.Any(p => p.Key == id))
            {
                return this.JsonResponse(People.FirstOrDefault(p => p.Key == id));
            }
        }
        catch (Exception ex)
        {
            return this.JsonExceptionResponse(ex);
        }
    }
    
    // You can override the default headers and add custom headers to each API Response.
    public override void SetDefaultHeaders() => this.NoCache();
}
```

*Or if you want to use the Wildcard strategy (which is the default):*

```csharp
public class PeopleController : WebApiController
{
    public PeopleController(IHttpContext context)
    : base(context)
    {
    }

    [WebApiHandler(HttpVerbs.Get, "/api/people/*")]
    public bool GetPeople()
    {
        try
        {
            var lastSegment = this.Request.Url.Segments.Last();
            if (lastSegment.EndsWith("/"))
                return this.JsonResponse(People);

            int key = 0;
            if (int.TryParse(lastSegment, out key) && People.Any(p => p.Key == key))
            {
                return this.JsonResponse(People.FirstOrDefault(p => p.Key == key));
            }

            throw new KeyNotFoundException("Key Not Found: " + lastSegment);
        }
        catch (Exception ex)
        {
            return this.JsonExceptionResponse(ex);
        }
    }
}
```

The `SetDefaultHeaders` method will add a no-cache policy to all Web API responses. If you plan to handle a differente policy or even custom headers to each different Web API method we recommend you override this method as you need.

## WebSockets Example:

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

    /// <inheritdoc/>
    public override string ServerName => "Chat Server"

    protected override void OnMessageReceived(WebSocketContext context, byte[] rxBuffer, WebSocketReceiveResult rxResult)
    {
        var session = this.WebServer.GetSession(context);
        foreach (var ws in this.WebSockets)
        {
            if (ws != context)
                this.Send(ws, rxBuffer.ToText());
        }
    }

    protected override void OnClientConnected(WebSocketContext context)
    {
        this.Send(context, "Welcome to the chat room!");
        
        foreach (var ws in this.WebSockets)
        {
            if (ws != context)
                this.Send(ws, "Someone joined the chat room.");
        }
    }
    protected override void OnFrameReceived(WebSocketContext context, byte[] rxBuffer, WebSocketReceiveResult rxResult)
    {
        return;
    }
    protected override void OnClientDisconnected(WebSocketContext context)
    {
        this.Broadcast("Someone left the chat room.");
    }
}
```

## Related Projects and Nugets

Name | Author | Description
-----|--------|--------------
[Butterfly.EmbedIO](https://www.nuget.org/packages/Butterfly.EmbedIO/) | Fireshark Studios, LLC | Implementation of Butterfly.Core.Channel and Butterfly.Core.WebApi using the EmbedIO server
[embedio-cli](https://github.com/unosquare/embedio-cli) | Unosquare | A dotnet global tool that enables start any web folder or EmbedIO assembly (WebAPI or WebSocket) from command line.
[EmbedIO.BearerToken](https://www.nuget.org/packages/EmbedIO.BearerToken/)  | Unosquare | Allow to authenticate with a Bearer Token. It uses a Token endpoint (at /token path) and with a defined validation delegate create a JsonWebToken. The module can check all incoming requests or a paths
[EmbedIO.LiteLibWebApi](https://www.nuget.org/packages/EmbedIO.LiteLibWebApi/) | Unosquare | Allow to expose a sqlite database as REST api using EmbedIO WebApi and LiteLib libraries
[EmbedIO.OWIN](https://www.nuget.org/packages/EmbedIO.OWIN/) | Unosquare | EmbedIO can use the OWIN platform in two different approach: You can use EmbedIO as OWIN server and use all OWIN framework with EmbedIO modules.
[Microsoft.AspNetCore.Server.EmbedIO](https://www.nuget.org/packages/Microsoft.AspNetCore.Server.EmbedIO/) | Dju  | EmbedIO web server support for ASP.NET Core, as a drop-in replacement for Kestrel

## Notes
[1] - EmbedIO uses lowercase URL parts. In Windows systems, this is the expected behavior but in Unix systems using MONO please refer to [Mono IOMap](http://www.mono-project.com/docs/advanced/iomap/) if you want to work with case-insensitive URL parts.
