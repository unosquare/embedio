namespace Unosquare.Labs.EmbedIO.Command
{
    using Swan;
    using System;
    using System.IO;
    using System.Linq;
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

            Runtime.WriteWelcomeBanner();

            if (!Runtime.ArgumentParser.ParseArguments(args, options)) return;

            "Press any key to stop the server.".Info();
            
            using (var server = new WebServer($"http://localhost:{options.Port}/"))
            {
                server.WithLocalSession();

                server.EnableCors().WithStaticFolderAt(SearchForWwwRootFolder(options.RootPath));
                //server.EnableCors().WithStaticFolderAt(options.RootPath,
                //    defaultDocument: Properties.Settings.Default.HtmlDefaultDocument);

                //server.Module<StaticFilesModule>().DefaultExtension = Properties.Settings.Default.HtmlDefaultExtension;
                //server.Module<StaticFilesModule>().UseRamCache = Properties.Settings.Default.UseRamCache;

                if (string.IsNullOrEmpty(options.ApiAssemblies) == false)
                {
                    $"Registering Assembly {options.ApiAssemblies}".Debug();
                    LoadApi(server, options.ApiAssemblies);
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
        private static void LoadApi(WebServer server, string apiPath)
        {
            try
            {
                "Assembly LoadApi".WriteLine(ConsoleColor.Yellow);

                var assembly = Assembly.LoadFrom(apiPath);

                server.LoadApiControllers(assembly).LoadWebSockets(assembly);
            }
            catch (FileNotFoundException fnfex) {
                $"Assembly FileNotFoundException {fnfex.Message}".Debug();
            }
            catch (Exception ex)
            {
                ex.Log(nameof(Program));
            }
        }

        private static string SearchForWwwRootFolder(string rootPath)
        {
            var wwwroot = "wwwroot";
            if (rootPath.Equals(wwwroot)) return rootPath;

            var wwwrootpath = Path.Combine(rootPath, wwwroot);
            if (Directory.Exists(wwwrootpath))
            {
                $"Serving: {wwwrootpath}".Debug();
                return wwwrootpath;
            }
            
            return rootPath;
        }
    }
}
