namespace Unosquare.Labs.EmbedIO.Command
{
    using CommandLine;
    using System;
    using System.Reflection;
    using Unosquare.Labs.EmbedIO.Modules;

    /// <summary>
    /// Entry poing
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Load WebServer instance
        /// </summary>
        /// <param name="args"></param>
        private static void Main(string[] args)
        {
            var options = new Options();

            Console.WriteLine("Unosquare.Labs.EmbedIO Web Server");

            if (!Parser.Default.ParseArguments(args, options)) return;

            Console.WriteLine("  Command-Line Utility: Press any key to stop the server.");

            var serverUrl = "http://localhost:" + options.Port + "/";
            using (
                var server = options.NoVerbose
                    ? WebServer.Create(serverUrl)
                    : WebServer.CreateWithConsole(serverUrl))
            {
                if (Properties.Settings.Default.UseLocalSessionModule)
                    server.WithLocalSession();

                server.EnableCors().WithStaticFolderAt(options.RootPath,
                    defaultDocument: Properties.Settings.Default.HtmlDefaultDocument);

                server.Module<StaticFilesModule>().DefaultExtension = Properties.Settings.Default.HtmlDefaultExtension;
                server.Module<StaticFilesModule>().UseRamCache = Properties.Settings.Default.UseRamCache;

                if (options.ApiAssemblies != null && options.ApiAssemblies.Count > 0)
                {
                    foreach (var api in options.ApiAssemblies)
                    {
                        server.Log.DebugFormat("Registering Assembly {0}", api);
                        LoadApi(api, server);
                    }
                }

                // start the server
                server.RunAsync();
                Console.ReadKey(true);
            }
        }

        /// <summary>
        /// Load an Assembly
        /// </summary>
        /// <param name="apiPath"></param>
        /// <param name="server"></param>
        private static void LoadApi(string apiPath, WebServer server)
        {
            try
            {
                var assembly = Assembly.LoadFile(apiPath);

                if (assembly == null) return;

                server.LoadApiControllers(assembly).LoadWebSockets(assembly);
            }
            catch (Exception ex)
            {
                server.Log.Error(ex.Message);
                server.Log.Error(ex.StackTrace);
            }
        }
    }
}