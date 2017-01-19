namespace Unosquare.Labs.EmbedIO.Command
{
    using Swan;
    using System;
#if NET452
    using System.Reflection;
#else
    using System.Runtime.Loader;
#endif

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

            Runtime.WriteWelcomeBanner();

            if (!Swan.Components.ArgumentParser.Default.ParseArguments(args, options)) return;

            "Press any key to stop the server.".Info();

            var serverUrl = "http://localhost:" + options.Port + "/";

            using (var server = new WebServer(serverUrl))
            {
                // TODO: Add AppSettings file
                //if (Properties.Settings.Default.UseLocalSessionModule)
                server.WithLocalSession();

                server.EnableCors().WithStaticFolderAt(options.RootPath);
                //server.EnableCors().WithStaticFolderAt(options.RootPath,
                //    defaultDocument: Properties.Settings.Default.HtmlDefaultDocument);

                //server.Module<StaticFilesModule>().DefaultExtension = Properties.Settings.Default.HtmlDefaultExtension;
                //server.Module<StaticFilesModule>().UseRamCache = Properties.Settings.Default.UseRamCache;

                if (string.IsNullOrEmpty(options.ApiAssemblies))
                {
                    $"Registering Assembly {options.ApiAssemblies}".Debug();
                    LoadApi(options.ApiAssemblies, server);
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
#if NET452
                var assembly = Assembly.LoadFile(apiPath);
#else
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(apiPath);
#endif
                if (assembly == null) return;

                server.LoadApiControllers(assembly).LoadWebSockets(assembly);
            }
            catch (Exception ex)
            {
                ex.Log(nameof(Program));
            }
        }
    }
}