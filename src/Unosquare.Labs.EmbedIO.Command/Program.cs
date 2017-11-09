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
            args = new[] {"-p", @"c:\\Unosquare", "-o", "5588"};

            var options = new Options();

            Runtime.WriteWelcomeBanner();

            if (!Runtime.ArgumentParser.ParseArguments(args, options)) return;

            "Press any key to stop the server.".Info();
            
            using (var server = new WebServer($"http://localhost:{options.Port}/"))
            {
                // TODO: Add AppSettings file
                //if (Properties.Settings.Default.UseLocalSessionModule)
                server.WithLocalSession();

                server.EnableCors().WithStaticFolderAt(options.RootPath, useDirectoryBrowser: true);
                //server.EnableCors().WithStaticFolderAt(options.RootPath,
                //    defaultDocument: Properties.Settings.Default.HtmlDefaultDocument);

                //server.Module<StaticFilesModule>().DefaultExtension = Properties.Settings.Default.HtmlDefaultExtension;
                //server.Module<StaticFilesModule>().UseRamCache = Properties.Settings.Default.UseRamCache;

                if (string.IsNullOrEmpty(options.ApiAssemblies) == false)
                {
                    $"Registering Assembly {options.ApiAssemblies}".Debug();
                    LoadApi(options.ApiAssemblies, server);
                }

                // start the server
                server.RunAsync();
                Console.ReadKey();
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

                server.LoadApiControllers(assembly).LoadWebSockets(assembly);
            }
            catch (Exception ex)
            {
                ex.Log(nameof(Program));
            }
        }
    }
}