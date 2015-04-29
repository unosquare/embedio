namespace Unosquare.Labs.EmbedIO.Command
{
    using CommandLine;
    using System;
    using System.Linq;
    using System.Reflection;
    using Unosquare.Labs.EmbedIO.Log;
    using Unosquare.Labs.EmbedIO.Modules;

    internal class Program
    {
        private static readonly SimpleConsoleLog Log = new SimpleConsoleLog();

        private static void Main(string[] args)
        {
            var options = new Options();

            Console.WriteLine("Unosquare.Labs.EmbedIO Web Server");

            if (!Parser.Default.ParseArguments(args, options)) return;

            Console.WriteLine("  Command-Line Utility: Press any key to stop the server.");

            using (var server = new WebServer(Properties.Settings.Default.ServerAddress, Log))
            {
                if (Properties.Settings.Default.UseLocalSessionModule)
                    server.RegisterModule(new LocalSessionModule());

                var staticFilesModule = new StaticFilesModule(options.RootPath)
                {
                    DefaultDocument = Properties.Settings.Default.HtmlDefaultDocument,
                    DefaultExtension = Properties.Settings.Default.HtmlDefaultExtension,
                    UseRamCache = Properties.Settings.Default.UseRamCache
                };

                server.RegisterModule(staticFilesModule);

                if (options.ApiAssemblies != null && options.ApiAssemblies.Count > 0)
                {
                    foreach (var api in options.ApiAssemblies)
                    {
                        Log.DebugFormat("Checking API {0}", api);
                        LoadApi(api, server);
                    }
                }

                // start the server
                server.RunAsync();
                Console.ReadKey(true);
            }
        }

        private static void LoadApi(string apiPath, WebServer server)
        {
            try
            {
                var assembly = Assembly.LoadFile(apiPath);

                if (assembly == null) return;

                var types = assembly.GetTypes();

                // Load WebApiModules
                var apiControllers =
                    types.Where(x => x.IsClass && !x.IsAbstract && x.IsSubclassOf(typeof (WebApiController))).ToArray();

                if (apiControllers.Any())
                {
                    server.RegisterModule(new WebApiModule());

                    foreach (var apiController in apiControllers)
                    {
                        server.Module<WebApiModule>().RegisterController(apiController);
                        Log.DebugFormat("Registering {0} WebAPI", apiController.Name);
                    }
                }
                else
                {
                    Log.DebugFormat("{0} does not have any WebAPI", apiPath);
                }

                // Load WebSocketsModules
                var sockerServers = types.Where(x => x.BaseType == typeof (WebSocketsServer)).ToArray();

                if (sockerServers.Any())
                {
                    server.RegisterModule(new WebSocketsModule());

                    foreach (var socketServer in sockerServers)
                    {
                        server.Module<WebSocketsModule>().RegisterWebSocketsServer(socketServer);
                        Log.DebugFormat("Registering {0} WebSocket", socketServer.Name);
                    }
                }
                else
                {
                    Log.DebugFormat("{0} does not have any WebSocket", apiPath);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }
    }
}