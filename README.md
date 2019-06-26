[![Codacy Badge](https://api.codacy.com/project/badge/Grade/e24c50205c4e486dbbe2b734a790b751)](https://app.codacy.com/app/UnosquareLabs/embedio?utm_source=github.com&utm_medium=referral&utm_content=unosquare/embedio&utm_campaign=Badge_Grade_Settings)
 [![Analytics](https://ga-beacon.appspot.com/UA-8535255-2/unosquare/embedio/)](https://github.com/igrigorik/ga-beacon)
 [![Build status](https://ci.appveyor.com/api/projects/status/w59t7sct3a8ir96t?svg=true)](https://ci.appveyor.com/project/geoperez/embedio)
 [![Build Status](https://travis-ci.org/unosquare/embedio.svg?branch=master)](https://travis-ci.org/unosquare/embedio)
 [![NuGet version](https://badge.fury.io/nu/embedio.svg)](https://www.nuget.org/packages/Embedio)
 [![NuGet](https://img.shields.io/nuget/dt/embedio.svg)](https://www.nuget.org/packages/Embedio)
[![Coverage Status](https://coveralls.io/repos/unosquare/embedio/badge.svg?branch=master)](https://coveralls.io/r/unosquare/embedio?branch=master)
[![BuiltWithDotnet](https://builtwithdot.net/project/105/embedio/badge)](https://builtwithdot.net/project/105/embedio)
[![Slack](https://img.shields.io/badge/chat-slack-blue.svg)](https://join.slack.com/t/embedio/shared_invite/enQtNjcwMjgyNDk4NzUzLWQ4YTE2MDQ2MWRhZGIyMTRmNTU0YmY4MmE3MTJmNTY4MmZiZDAzM2M4MTljMmVmNjRiZDljM2VjYjI5MjdlM2U)

![EmbedIO](https://raw.githubusercontent.com/unosquare/embedio/master/images/embedio.png)

*:star: Please star this project if you find it useful!*

**This README is for EmbedIO v2.x. Click [here](https://github.com/unosquare/embedio/tree/v1.X) if you are still using EmbedIO v1.x.**

- [Overview](#overview)
    - [EmbedIO 2.0 - What's new](#embedio-20---whats-new)
    - [Some usage scenarios](#some-usage-scenarios)
- [Installation](#installation)
- [Usage](#usage)
    - [WebServer Setup](#webserver-setup)
    - [IHttpContext Extension Methods](#ihttpcontext-extension-methods)
    - [Easy Routes](#easy-routes)
    - [Serving Files from Assembly](#serving-files-from-assembly)
- [Support for SSL](#support-for-ssl)
- [Examples](#examples)
    - [Basic Example](#basic-example)
    - [REST API Example](#rest-api-example)
    - [WebSockets Example](#websockets-example)
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
* Supports HTTP 206 Partial Content
* Support [Xamarin Forms](https://github.com/unosquare/embedio/tree/master/src/EmbedIO.Forms.Sample)
* And many more options in the same package

### EmbedIO 2.0 - What's new

#### Breaking changes
* `WebApiController` is renewed. Reduce the methods overhead removing the WebServer and Context arguments. See examples below.
* `RoutingStrategy.Regex` is the default routing scheme.

#### Additional changes
* `IHttpListener` is runtime/platform independent, you can choose Unosquare `HttpListener` implementation with NET472 or NETSTANDARD20. This separation of implementations brings new access to interfaces from common Http objects like `IHttpRequest`, `IHttpContext` and more.
* `IWebServer` is a new interface to create custom web server implementation, like a Test Web Server where all the operations are in-memory to speed up unit testing. Similar to [TestServer from OWIN](https://msdn.microsoft.com/en-us/library/microsoft.owin.testing.testserver(v=vs.113).aspx)
* General improvements in how the Unosquare `HttpListner` is working and code clean-up.

*Note* - We encourage to upgrade to the newest EmbedIO version. Branch version 1.X will no longer be maintained, and issues will be tested against 2.X and resolved just there.

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

### WebServer Setup

### IHttpContext Extension Methods

By adding the namespace `Unosquare.Labs.EmbedIO` to your class, you can use some helpful extension methods for `IHttpContext`, `IHttpResponse` and `IHttpRequest`. These methods can be used in any Web module (like [Fallback Module](https://unosquare.github.io/embedio/api/Unosquare.Labs.EmbedIO.Modules.FallbackModule.html)) or inside a [WebAPI Controller](https://unosquare.github.io/embedio/api/Unosquare.Labs.EmbedIO.Modules.WebApiController.html) method.

Below, some common scenarios using a WebAPI Controller method as body function:

#### Reading from a POST body as a dictionary (application/x-www-form-urlencoded)

For reading a dictionary from a HTTP Request body you can use [RequestFormDataDictionaryAsync](https://unosquare.github.io/embedio/api/Unosquare.Labs.EmbedIO.Extensions.html#Unosquare_Labs_EmbedIO_Extensions_RequestFormDataDictionaryAsync_Unosquare_Labs_EmbedIO_IHttpContext_). This method works directly from `IHttpContext` and returns the key-value pairs sent by using the Contet-Type 'application/x-www-form-urlencoded'.

```csharp
    [WebApiHandler(HttpVerbs.Post, "/api/data")]
    public async Task<bool> PostData() 
    {
        var data = await HttpContext.RequestFormDataDictionaryAsync();
	// Perform an operation with the data
	await SaveData(data);
	
	return true;
    }
```

#### Reading from a POST body as a JSON payload (application/json)

For reading a JSON payload and deserialize it to an object from a HTTP Request body you can use [ParseJson<T>](https://unosquare.github.io/embedio/api/Unosquare.Labs.EmbedIO.Extensions.html#Unosquare_Labs_EmbedIO_Extensions_ParseJsonAsync__1_Unosquare_Labs_EmbedIO_IHttpContext_). This method works directly from `IHttpContext` and returns an object of the type specified in the generic type.

```csharp
    [WebApiHandler(HttpVerbs.Post, "/api/data")]
    public async Task<bool> PostJsonData() 
    {
        var data = HttpContext.ParseJson<MyData>();
	// Perform an operation with the data
	await SaveData(data);
	
	return true;
    }
```

#### Reading from a POST body as a FormData (multipart/form-data)

EmbedIO doesn't provide the functionality to read from a Multipart FormData stream. But you can check the [HttpMultipartParser Nuget](https://www.nuget.org/packages/HttpMultipartParser/) and connect the Request input directly to the HttpMultipartParser, very helpful and small library.

There is [another solution](http://stackoverflow.com/questions/7460088/reading-file-input-from-a-multipart-form-data-post) but it requires this [Microsoft Nuget](https://www.nuget.org/packages/Microsoft.AspNet.WebApi.Client).

#### Writing a binary stream

For writing a binary stream directly to the Response Output Stream you can use [BinaryResponseAsync](https://unosquare.github.io/embedio/api/Unosquare.Labs.EmbedIO.Extensions.html#Unosquare_Labs_EmbedIO_Extensions_BinaryResponseAsync_Unosquare_Labs_EmbedIO_IHttpContext_System_IO_Stream_System_Boolean_System_Threading_CancellationToken_). This method has an overload to use `IHttpContext` and you need to set the Content-Type beforehand.

```csharp
    [WebApiHandler(HttpVerbs.Get, "/api/binary")]
    public async Task<bool> GetBinary() 
    {
        var stream = new MemoryStream();
	
	// Call a fictional external source
	await GetExternalStream(stream);
	
	return await HttpContext.BinaryResponseAsync(stream);
    }
```

### Easy Routes

### Serving Files from Assembly

You can use files from Assembly Resources directly with EmbedIO. They will be served as local files. This is a good practice when you want to provide a web server solution in a single file. 

First, you need to add the `ResourceFilesModule` module to your `IWebServer`. The `ResourceFilesModule` constructor takes two arguments, the Assembly reference where the Resources are located and the path to the Resources (Usually this path is the Assembly name plus the word "Resources").

```csharp
using (var server = new WebServer(url)) 
{
	server.RegisterModule(new ResourceFilesModule(typeof(MyProgram).Assembly,
                        "Unosquare.MyProgram.Resources"));
	
	// Continue with the server set up and initialization
}
```

And that's all. The module will read the files in the Assembly using the second argument as the base path. For example, if you have a folder containing an image, the resource path can be `Unosquare.MyProgram.Resources.MyFolder.Image.jpg` and the relative URL is `/MyFolder/Image.jpg`.

## Support for SSL

Both HTTP listeners (Microsoft and Unosquare) can open a web server using SSL. This support is for Windows only (for now) and you need to manually register your certificate or use the `WebServerOptions` class to initialize a new `WebServer` instance. This section will provide some examples of how to use SSL but first a brief explanation of how SSL works on Windows.

For Windows Vista or better, Microsoft provides Network Shell (`netsh`). This command line tool allows to map an IP-port to a certificate, so incoming HTTP request can upgrade the connection to a secure stream using the provided certificate. EmbedIO can read or register certificates to a default store (My/LocalMachine) and use them against a netsh `sslcert` for binding the first `https` prefix registered.

For Windows XP and Mono, you can use manually the `httpcfg` for registering the binding.

### Using a PFX file and AutoRegister option

The more practical case to use EmbedIO with SSL is the `AutoRegister` option. You need to create a `WebServerOptions` instance with the path to a PFX file and the `AutoRegister` flag on. This options will try to get or register the certificate to the default certificate store. Then it will use the certificate thumbprint to register with `netsh` the FIRST `https` prefix registered on the options.

### Using AutoLoad option

If you already have a certificate on the default certificate store and the binding is also registered in `netsh`, you can use `Autoload` flag and optionally provide a certificate thumbprint. If the certificate thumbprint is not provided, EmbedIO will read the data from `netsh`. After getting successfully the certificate from the store, the raw data is passed to the WebServer.

## Examples

### Basic Example

Please note the comments are the important part here. More info is available in the samples.

```csharp
namespace Unosquare
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
                server.WithLocalSession();

                // Here we setup serving of static files
                server.RegisterModule(new StaticFilesModule("c:/web"));
                // The static files module will cache small files in ram until it detects they have been modified.
                server.Module<StaticFilesModule>().UseRamCache = true;

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

### REST API Example

The WebApi module supports two routing strategies: Wildcard and Regex. By default, the WebApi module will use the **Regex Routing Strategy** trying to match and resolve the values from a route template, in a similar fashion to Microsoft's Web API. 

**Note** - Wilcard routing will be dropped in the next major version of EmbedIO. We advise to use Regex only.

A method with the following route `/api/people/{id}` is going to match any request URL with three segments: the first two `api` and `people` and the last 
one is going to be parsed or converted to the type in the `id` argument of the handling method signature. Please read on if this was confusing as it is 
much simpler than it sounds. Additionally, you can put multiple values to match, for example `/api/people/{mainSkill}/{age}`, and receive the 
parsed values from the URL straight into the arguments of your handler method.

During server setup:

```csharp
var server =  new WebServer("http://localhost:9696/", RoutingStrategy.Regex);

server.RegisterModule(new WebApiModule());
server.Module<WebApiModule>().RegisterController<PeopleController>();
```

And our controller class (using default Regex Strategy) looks like:

```csharp
// A controller is a class where the WebApi module will find available
// endpoints. The class must extend WebApiController.
public class PeopleController : WebApiController
{
    // You need to add a default constructor where the first argument
    // is an IHttpContext
    public PeopleController(IHttpContext context)
        : base(context)
    {
    }

    // You need to include the WebApiHandler attribute to each method
    // where you want to export an endpoint. The method should return
    // bool or Task<bool>.
    [WebApiHandler(HttpVerbs.Get, "/api/people/{id}")]
    public async Task<bool> GetPersonById(int id)
    {
        try
        {
            // This is fake call to a Repository
            var person = await PeopleRepository.GetById(id);
            return await Ok(person);
        }
        catch (Exception ex)
        {
            return await InternalServerError(ex);
        }
    }
    
    // You can override the default headers and add custom headers to each API Response.
    public override void SetDefaultHeaders() => HttpContext.NoCache();
}
```

The `SetDefaultHeaders` method will add a no-cache policy to all Web API responses. If you plan to handle a differente policy or even custom headers to each different Web API method we recommend you override this method as you need.

The previous default strategy (Wildcard) matches routes using the asterisk `*` character in the route. **For example:** 

- The route `/api/people/*` will match any request with a URL starting with the two first URL segments `api` and 
`people` and ending with anything. The route `/api/people/hello` will be matched.
- You can also use wildcards in the middle of the route. The route `/api/people/*/details` will match requests 
starting with the two first URL segments `api` and `people`, and end with a `details` segment. The route `/api/people/hello/details` will be matched. 

During server setup:

```csharp
var server =  new WebServer("http://localhost:9696/", RoutingStrategy.Regex);

server.RegisterModule(new WebApiModule());
server.Module<WebApiModule>().RegisterController<PeopleController>();
```

```csharp
public class PeopleController : WebApiController
{
    public PeopleController(IHttpContext context)
    : base(context)
    {
    }

    [WebApiHandler(HttpVerbs.Get, "/api/people/*")]
    public async Task<bool> GetPeopleOrPersonById()
    {
        var lastSegment = Request.Url.Segments.Last();

        // If the last segment is a backslash, return all
        // the collection. This endpoint call a fake Repository.
        if (lastSegment.EndsWith("/"))
            return await Ok(await PeopleRepository.GetAll());
                
        if (int.TryParse(lastSegment, out var id))
        {
            return await Ok(await PeopleRepository.GetById(id));
        }

        throw new KeyNotFoundException("Key Not Found: " + lastSegment);
    }
}
```

### WebSockets Example

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
        : base(true)
    {
        // placeholder
    }

    public override string ServerName => "Chat Server";

    protected override void OnMessageReceived(IWebSocketContext context, byte[] rxBuffer, IWebSocketReceiveResult rxResult)
    {
        foreach (var ws in WebSockets)
        {
            if (ws != context)
                Send(ws, rxBuffer.ToText());
        }
    }

    protected override void OnClientConnected(
        IWebSocketContext context,
        System.Net.IPEndPoint localEndPoint,
        System.Net.IPEndPoint remoteEndPoint)
    {
        Send(context, "Welcome to the chat room!");
        
        foreach (var ws in WebSockets)
        {
            if (ws != context)
                Send(ws, "Someone joined the chat room.");
        }
    }

    protected override void OnFrameReceived(IWebSocketContext context, byte[] rxBuffer, IWebSocketReceiveResult rxResult)
    {
        // placeholder
    }

    protected override void OnClientDisconnected(IWebSocketContext context)
    {
        Broadcast("Someone left the chat room.");
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
[SambaFetcher](https://github.com/nddipiazza/SambaFetcher/) | nddipiazza  | A .NET tool to connect a web server with Samba

## Special Thanks

 [![YourKit](https://www.yourkit.com/images/yklogo.png)](https://www.yourkit.com)

 To YourKit for supports open source projects with its full-featured [.NET Profiler](https://www.yourkit.com/.net/profiler/), an amazing tool to profile CPU and Memory!
