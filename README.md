 [![Analytics](https://ga-beacon.appspot.com/UA-8535255-2/unosquare/embedio/)](https://github.com/igrigorik/ga-beacon)
 [![Build status](https://ci.appveyor.com/api/projects/status/w59t7sct3a8ir96t?svg=true)](https://ci.appveyor.com/project/geoperez/embedio)
 ![Buils status](https://github.com/unosquare/embedio/workflows/.NET%20Core%20CI/badge.svg)
 [![NuGet version](https://badge.fury.io/nu/embedio.svg)](https://www.nuget.org/packages/embedio)
 [![NuGet](https://img.shields.io/nuget/dt/embedio.svg)](https://www.nuget.org/packages/embedio)
[![BuiltWithDotnet](https://builtwithdot.net/project/105/embedio/badge)](https://builtwithdot.net/project/105/embedio)
[![Slack](https://img.shields.io/badge/chat-slack-blue.svg)](https://join.slack.com/t/embedio/shared_invite/enQtNjcwMjgyNDk4NzUzLWQ4YTE2MDQ2MWRhZGIyMTRmNTU0YmY4MmE3MTJmNTY4MmZiZDAzM2M4MTljMmVmNjRiZDljM2VjYjI5MjdlM2U)

![EmbedIO](https://raw.githubusercontent.com/unosquare/embedio/master/images/embedio.png)

*:star: Please star this project if you find it useful!*

**This README is for EmbedIO v3.x. Click [here](https://github.com/unosquare/embedio/tree/v2.X) if you are still using EmbedIO v2.x.**

- [Overview](#overview)
    - [EmbedIO 3.0 - What's new](#embedio-30---whats-new)
    - [Some usage scenarios](#some-usage-scenarios)
- [Installation](#installation)
- [Usage](#usage)
    - [WebServer Setup](#webserver-setup)
    - [Reading from a POST body as a dictionary (application/x-www-form-urlencoded)](#reading-from-a-post-body-as-a-json-payload-applicationjson)
    - [Reading from a POST body as a JSON payload (application/json)](#reading-from-a-post-body-as-a-json-payload-applicationjson)
    - [Reading from a POST body as a FormData (multipart/form-data)](#reading-from-a-post-body-as-a-formdata-multipartform-data)
    - [Writing a binary stream](#writing-a-binary-stream)
    - [WebSockets Example](#websockets-example)
- [Support for SSL](#support-for-ssl)
- [Related Projects and Nugets](#related-projects-and-nugets)
- [Special Thanks](#special-thanks)

## Overview

A tiny, cross-platform, module based, MIT-licensed web server for .NET Framework and .NET Core.

* Written entirely in C#, using our helpful library [SWAN](https://github.com/unosquare/swan)
* Network operations use the async/await pattern: Responses are handled asynchronously
* Multiple implementations support: EmbedIO can use Microsoft `HttpListener` or internal Http Listener based on [Mono](https://www.mono-project.com/)/[websocket-sharp](https://github.com/sta/websocket-sharp/) projects
* Cross-platform: tested on multiple OS and runtimes. From Windows .NET Framework to Linux MONO.
* Extensible: Write your own modules -- For example, video streaming, UPnP, etc. Check out [EmbedIO Extras](https://github.com/unosquare/embedio-extras) for additional modules
* Small memory footprint
* Create REST APIs quickly with the out-of-the-box Web API module
* Serve static or embedded files with 1 line of code (also out-of-the-box)
* Handle sessions with the built-in LocalSessionWebModule
* WebSockets support
* CORS support. Origin, Header and Method validation with OPTIONS preflight
* HTTP 206 Partial Content support
* Support [Xamarin Forms](https://github.com/unosquare/embedio/tree/master/src/EmbedIO.Forms.Sample)
* And many more options in the same package

### EmbedIO 3.0 - What's new

The major version 3.0 includes a lot of changes in how the webserver process the incoming request and the pipeline of the Web Modules. You can check a complete list of changes and a upgrade guide for v2 users [here](https://github.com/unosquare/embedio/wiki/Upgrade-from-v2).

### Some usage scenarios:

* Write a cross-platform GUI entirely using React/AngularJS/Vue.js or any Javascript framework
* Write a game using Babylon.js and make EmbedIO your serve your code and assets
* Create GUIs for Windows services or Linux daemons
* Works well with [LiteLib](https://github.com/unosquare/litelib) - add SQLite support in minutes!
* Write client applications with real-time communication between them using WebSockets
* Write internal web server for [Xamarin Forms](https://github.com/unosquare/embedio/tree/master/src/EmbedIO.Forms.Sample) applications

## Installation:

You can start using EmbedIO by just downloading the nuget.

### Package Manager

```
PM> Install-Package EmbedIO
```

### .NET CLI

```
> dotnet add package EmbedIO
```

## Usage

Working with EmbedIO is pretty simple, check the follow sections to start coding right away. You can find more useful recipes and implementation details in the [Cookbook](https://github.com/unosquare/embedio/wiki/Cookbook).

### WebServer Setup

Please note the comments are the important part here. More info is available in the samples.

```csharp
namespace Unosquare
{
    using System;
    using EmbedIO;
    using EmbedIO.WebApi;

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
            using (var server = CreateWebServer(url))
            {
                // Once we've registered our modules and configured them, we call the RunAsync() method.
                server.RunAsync();

                var browser = new System.Diagnostics.Process()
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }
                };
                browser.Start();
                // Wait for any key to be pressed before disposing of our web server.
                // In a service, we'd manage the lifecycle of our web server using
                // something like a BackgroundWorker or a ManualResetEvent.
                Console.ReadKey(true);
            }
        }
	
	// Create and configure our web server.
        private static WebServer CreateWebServer(string url)
        {
            var server = new WebServer(o => o
                    .WithUrlPrefix(url)
                    .WithMode(HttpListenerMode.EmbedIO))
		 // First, we will configure our web server by adding Modules.
                .WithLocalSessionManager()
                .WithWebApi("/api", m => m
                    .WithController<PeopleController>())
                .WithModule(new WebSocketChatModule("/chat"))
                .WithModule(new WebSocketTerminalModule("/terminal"))
                .WithStaticFolder("/", HtmlRootPath, true, m => m
                    .WithContentCaching(UseFileCache)) // Add static files after other modules to avoid conflicts
                .WithModule(new ActionModule("/", HttpVerbs.Any, ctx => ctx.SendDataAsync(new { Message = "Error" })));

            // Listen for state changes.
            server.StateChanged += (s, e) => $"WebServer New State - {e.NewState}".Info();

            return server;
        }
    }
}
```

### Reading from a POST body as a dictionary (application/x-www-form-urlencoded)

For reading a dictionary from an HTTP Request body inside a WebAPI method you can add an argument to your method with the attribute `FormData`.

```csharp
    [Route(HttpVerbs.Post, "/data")]
    public async Task PostData([FormData] NameValueCollection data) 
    {
        // Perform an operation with the data
        await SaveData(data);
    }
```

### Reading from a POST body as a JSON payload (application/json)

For reading a JSON payload and deserialize it to an object from an HTTP Request body you can use [GetRequestDataAsync<T>](#). This method works directly from `IHttpContext` and returns an object of the type specified in the generic type.

```csharp
    [Route(HttpVerbs.Post, "/data")]
    public async Task PostJsonData() 
    {
        var data = HttpContext.GetRequestDataAsync<MyData>();
	
        // Perform an operation with the data
        await SaveData(data);
    }
```

### Reading from a POST body as a FormData (multipart/form-data)

EmbedIO doesn't provide the functionality to read from a Multipart FormData stream. But you can check the [HttpMultipartParser Nuget](https://www.nuget.org/packages/HttpMultipartParser/) and connect the Request input directly to the HttpMultipartParser, very helpful and small library.

A sample code using the previous library:

```csharp
    [Route(HttpVerbs.Post, "/upload")]
    public async Task UploadFile()
    {
        var parser = await MultipartFormDataParser.ParseAsync(Request.InputStream);
        // Now you can access parser.Files
    }
```

There is [another solution](http://stackoverflow.com/questions/7460088/reading-file-input-from-a-multipart-form-data-post) but it requires this [Microsoft Nuget](https://www.nuget.org/packages/Microsoft.AspNet.WebApi.Client).

### Writing a binary stream

You can open the Response Output Stream with the extension [OpenResponseStream]().

```csharp
    [Route(HttpVerbs.Get, "/binary")]
    public async Task GetBinary() 
    {
	// Call a fictional external source
	using (var stream = HttpContext.OpenResponseStream())
                await stream.WriteAsync(dataBuffer, 0, 0);
    }
```

### WebSockets Example

Working with WebSocket is pretty simple, you just need to implement the abstract class `WebSocketModule` and register the module to your Web server as follow:

```csharp
server..WithModule(new WebSocketChatModule("/chat"));
```

And our web sockets server class looks like:

```csharp
namespace Unosquare
{
    using EmbedIO.WebSockets;

    /// <summary>
    /// Defines a very simple chat server.
    /// </summary>
    public class WebSocketsChatServer : WebSocketModule
    {
        public WebSocketsChatServer(string urlPath)
            : base(urlPath, true)
        {
            // placeholder
        }

        /// <inheritdoc />
        protected override Task OnMessageReceivedAsync(
            IWebSocketContext context,
            byte[] rxBuffer,
            IWebSocketReceiveResult rxResult)
            => SendToOthersAsync(context, Encoding.GetString(rxBuffer));

        /// <inheritdoc />
        protected override Task OnClientConnectedAsync(IWebSocketContext context)
            => Task.WhenAll(
                SendAsync(context, "Welcome to the chat room!"),
                SendToOthersAsync(context, "Someone joined the chat room."));

        /// <inheritdoc />
        protected override Task OnClientDisconnectedAsync(IWebSocketContext context)
            => SendToOthersAsync(context, "Someone left the chat room.");

        private Task SendToOthersAsync(IWebSocketContext context, string payload)
            => BroadcastAsync(payload, c => c != context);
    }
}

```

## Support for SSL

Both HTTP listeners (Microsoft and Unosquare) can open a web server using SSL. This support is for Windows only (for now) and you need to manually register your certificate or use the `WebServerOptions` class to initialize a new `WebServer` instance. This section will provide some examples of how to use SSL but first a brief explanation of how SSL works on Windows.

For Windows Vista or better, Microsoft provides Network Shell (`netsh`). This command line tool allows to map an IP-port to a certificate, so incoming HTTP request can upgrade the connection to a secure stream using the provided certificate. EmbedIO can read or register certificates to a default store (My/LocalMachine) and use them against a netsh `sslcert` for binding the first `https` prefix registered.

For Windows XP and Mono, you can use manually the `httpcfg` for registering the binding.

### Using a PFX file and AutoRegister option

The more practical case to use EmbedIO with SSL is the `AutoRegister` option. You need to create a `WebServerOptions` instance with the path to a PFX file and the `AutoRegister` flag on. This options will try to get or register the certificate to the default certificate store. Then it will use the certificate thumbprint to register with `netsh` the FIRST `https` prefix registered on the options.

### Using AutoLoad option

If you already have a certificate on the default certificate store and the binding is also registered in `netsh`, you can use `Autoload` flag and optionally provide a certificate thumbprint. If the certificate thumbprint is not provided, EmbedIO will read the data from `netsh`. After getting successfully the certificate from the store, the raw data is passed to the WebServer.

## Related Projects and Nugets

Name | Author | Description
-----|--------|--------------
[Butterfly.EmbedIO](https://www.nuget.org/packages/Butterfly.EmbedIO/) | Fireshark Studios, LLC | Implementation of Butterfly.Core.Channel and Butterfly.Core.WebApi using the EmbedIO server
[embedio-cli](https://github.com/unosquare/embedio-cli) | Unosquare | A dotnet global tool that enables start any web folder or EmbedIO assembly (WebAPI or WebSocket) from command line.
[EmbedIO.BearerToken](https://www.nuget.org/packages/EmbedIO.BearerToken/)  | Unosquare | Allow to authenticate with a Bearer Token. It uses a Token endpoint (at /token path) and with a defined validation delegate create a JsonWebToken. The module can check all incoming requests or a paths
[EmbedIO.LiteLibWebApi](https://www.nuget.org/packages/EmbedIO.LiteLibWebApi/) | Unosquare | Allow to expose a sqlite database as REST api using EmbedIO WebApi and LiteLib libraries
[EmbedIO.OWIN](https://www.nuget.org/packages/EmbedIO.OWIN/) | Unosquare | EmbedIO can use the OWIN platform in two different approach: You can use EmbedIO as OWIN server and use all OWIN framework with EmbedIO modules.
[Microsoft.AspNetCore.Server.EmbedIO](https://www.nuget.org/packages/Microsoft.AspNetCore.Server.EmbedIO/) | Dju  | EmbedIO web server support for ASP.NET Core, as a drop-in replacement for Kestrel
[SambaFetcher](https://github.com/nddipiazza/SambaFetcher/) | nddipiazza  | A .NET tool to connect a web server with Samba
[SimpleW](https://stratdev3.github.io/SimpleW/) | stratdev3  | A .NET Core Web Server Library, inspired by EmbedIO

## Special Thanks

 [![YourKit](https://www.yourkit.com/images/yklogo.png)](https://www.yourkit.com)

 To YourKit for supports open source projects with its full-featured [.NET Profiler](https://www.yourkit.com/.net/profiler/), an amazing tool to profile CPU and Memory!
