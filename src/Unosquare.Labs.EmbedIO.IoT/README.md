To use EmbedIO in windows IoT devices as a background process you need to do as fallow.
For Visual Studio 2015.

Before all, you can start a good tutorials and documentation to setup <a href="https://developer.microsoft.com/en-us/windows/iot">Windows IoT Core</a>.

## Configure the project

You need to have installed <a href="https://marketplace.visualstudio.com/items?itemName=MicrosoftIoT.WindowsIoTCoreProjectTemplates" target="_blank">Windows IoT project templates</a>.

**Start a new project**

* File > New > Project.
* Visual C# > Windows > Windows IoT Core > Background Application.

**Configure the Project**

* You need to update the Microsoft.NETCore.UniversalWindowsPlataform to the version 5.2.3 in VS 2015.
* Of course install EmbedIO nuget package.

## Start Coding

If you have all done and fallow the tutorial to setup and deploy a project to your device, all that rest is start coding.

### Start the Web Server

In the StartupTask class.

```csharp
// Use deferral to prevent the task from closing prematurely when using async methods
var deferral = taskInstance.GetDeferral();

// Instance the WebServe
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


