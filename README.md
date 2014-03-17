EmbedIO - Unosquare Labs
========================

A tiny, cross-platform, module based web server

* Cross-platform (tested in Mono 2.10.x)
* Extensible (Write your own modules)
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
    using System;
    using Unosquare.Labs.EmbedIO;
    using Unosquare.Labs.EmbedIO.Modules;

    class Program
    {
        static void Main(string[] args)
        {
            using (var server = new WebServer("http://localhost:9696/"))
            {
                server.Modules.Add(new LocalSessionWebModule());
                server.Modules.Add(new StaticFilesWebModule("/path/to/your/html");
                server.Run();
                Console.ReadKey(true);
            }
        }
    }
}
```
