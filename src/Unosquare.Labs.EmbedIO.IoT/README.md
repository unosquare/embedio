To use EmbedIO as a background process with devices using Windows IoT Core , you need to do as follow.
For Visual Studio 2015.

First of all, you can see the tutorials and documentation to start a project in [Windows IoT Core](https://developer.microsoft.com/en-us/windows/iot).

## Configure the project

You need to have installed [Windows IoT project templates](https://marketplace.visualstudio.com/items?itemName=MicrosoftIoT.WindowsIoTCoreProjectTemplates)

**Start a new project**

* File > New > Project.
* Visual C# > Windows > Windows IoT Core > Background Application.

**Configure the Project**

* You need to update the Microsoft.NETCore.UniversalWindowsPlataform to the version 5.2.3 for VS 2015.
* Of course install EmbedIO nuget package.

## Start Coding

If you have already follow the tutorial to setup and deploy a project to your device, all that rest is start coding.

### Start the Web Server

In the StartupTask class.

```csharp
// Use deferral to prevent the task from closing prematurely when using async methods
var deferral = taskInstance.GetDeferral();

// Instance the WebServer
var server = new WebServer(9090);

// Handle the Fallback to show a message
server.RegisterModule(new FallbackModule((ctx, ct) =>
{
    ctx.JsonResponse(new { Hola = "Message" });
    return true;
}));

// To run the server Async
await server.RunAsync();

// Once the asyn task is finish
deferral.Complete();
```

## Notes
[1] - Embedio does not support websockets yet