namespace Unosquare.Labs.EmbedIO.Samples
{
    using System.IO;
    using System.Reflection;
    using Modules;

    /// <summary>
    /// Sample helper
    /// </summary>
    public static class StaticFilesSample
    {
        /// <summary>
        /// Gets the HTML root path.
        /// </summary>
        /// <value>
        /// The HTML root path.
        /// </value>
        public static string HtmlRootPath
        {
            get
            {
                var assemblyPath = Path.GetDirectoryName(typeof(Program).GetTypeInfo().Assembly.Location);

                // This lets you edit the files without restarting the server.
#if DEBUG && !MONO
                return Path.Combine(Directory.GetParent(assemblyPath).Parent.Parent.FullName, "html");
#else
                // This is when you have deployed the server.
                return Path.Combine(assemblyPath, "html");
#endif
            }
        }

        /// <summary>
        /// Setups the specified server.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="useGzip">if set to <c>true</c> [use gzip].</param>
        public static void Setup(WebServer server, bool useGzip)
        {
            server.RegisterModule(new StaticFilesModule(HtmlRootPath));
            // The static files module will cache small files in ram until it detects they have been modified.
            server.Module<StaticFilesModule>().UseRamCache = false;
            server.Module<StaticFilesModule>().DefaultExtension = ".html";
            server.Module<StaticFilesModule>().UseGzip = useGzip;
            // We don't need to add the line below. The default document is always index.html.
            //server.Module<StaticFilesWebModule>().DefaultDocument = "index.html";
        }
    }
}