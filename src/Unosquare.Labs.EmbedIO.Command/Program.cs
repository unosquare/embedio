namespace Unosquare.Labs.EmbedIO.Command
{
    using Swan;
    using System;
    using System.Reflection;

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

            CurrentApp.WriteWelcomeBanner();

            if (!Swan.Runtime.ArgumentParser.Default.ParseArguments(args, options)) return;

            "  Command-Line Utility: Press any key to stop the server.".Info();

            var serverUrl = "http://localhost:" + options.Port + "/";

            using (
                var server = options.NoVerbose
                    ? WebServer.Create(serverUrl)
                    : WebServer.CreateWithConsole(serverUrl))
            {
                // TODO: Add AppSettings file
                //if (Properties.Settings.Default.UseLocalSessionModule)
                //    server.WithLocalSession();

                server.EnableCors().WithStaticFolderAt(options.RootPath);
                //server.EnableCors().WithStaticFolderAt(options.RootPath,
                //    defaultDocument: Properties.Settings.Default.HtmlDefaultDocument);

                //server.Module<StaticFilesModule>().DefaultExtension = Properties.Settings.Default.HtmlDefaultExtension;
                //server.Module<StaticFilesModule>().UseRamCache = Properties.Settings.Default.UseRamCache;

                if (string.IsNullOrEmpty(options.ApiAssemblies))
                {
#if NET452
                    $"Registering Assembly {options.ApiAssemblies}".Debug();
                    LoadApi(options.ApiAssemblies, server);
#else
                    $"Error loading Assembly {options.ApiAssemblies}".Debug();
#endif
                }

                // start the server
                server.RunAsync();
                Terminal.ReadKey(true);
            }
        }

        // TODO: Check how to load a assembly from filename at NETCORE
#if NET452
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
#endif
    }
}