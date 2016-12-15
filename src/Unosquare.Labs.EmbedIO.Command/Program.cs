namespace Unosquare.Labs.EmbedIO.Command
{
    using CommandLine;
    using Swan;
    using System;
    using System.Reflection;
    using Unosquare.Labs.EmbedIO.Modules;

    /// <summary>
    /// Entry point
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

            "Unosquare.Labs.EmbedIO Web Server".Info();

            if (!Parser.Default.ParseArguments(args, options)) return;

            "  Command-Line Utility: Press any key to stop the server.".Info();

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
                        $"Registering Assembly {api}".Debug();
                        LoadApi(api, server);
                    }
                }

                // start the server
                server.RunAsync();
                Terminal.ReadKey(true);
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