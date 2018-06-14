namespace Unosquare.Labs.EmbedIO.Command
{
    using Swan;
    using System;
    using System.IO;
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

            var currentDirectory = Directory.GetCurrentDirectory();

            "Press any key to stop the server.".Info();

            using (var server = new WebServer($"http://localhost:{options.Port}/"))
            {
                server.WithLocalSession();
                server.EnableCors();

                // using static files
                if (options.RootPath != null || options.ApiAssemblies == null)
                    server.WithStaticFolderAt(options.RootPath ?? SearchForWwwRootFolder(currentDirectory));

                // Assemblies
                $"Registering Assembly {options.ApiAssemblies}".Debug();
                LoadApi(server, options.ApiAssemblies ?? currentDirectory);

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
                var fullPath = Path.GetFullPath(apiPath);

                if (Path.GetExtension(apiPath).Equals(".dll"))
                {
                    var assembly = Assembly.LoadFile(fullPath);
                    server.LoadApiControllers(assembly).LoadWebSockets(assembly);
                }
                else
                {
                    var files = Directory.GetFiles(fullPath, "*.dll");

                    foreach (var file in files)
                    {
                        var assembly = Assembly.LoadFile(file);
                        server.LoadApiControllers(assembly).LoadWebSockets(assembly);
                    }
                }
            }
            catch (FileNotFoundException fnfex)
            {
                $"Assembly FileNotFoundException {fnfex.Message}".Debug();
            }
            catch (Exception ex)
            {
                $"Assembly Exception {ex.Message}".Debug();
                ex.Log(nameof(Program));
            }
        }

        private static string SearchForWwwRootFolder(string rootPath)
        {
            var wwwrootpath = Path.Combine(rootPath, "wwwroot");
            return Directory.Exists(wwwrootpath) ? wwwrootpath : rootPath;
        }
    }
}
