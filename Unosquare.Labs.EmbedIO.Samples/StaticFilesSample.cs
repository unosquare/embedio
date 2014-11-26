namespace Unosquare.Labs.EmbedIO.Samples
{
    using System.IO;
    using Unosquare.Labs.EmbedIO.Modules;

    /// <summary>
    /// 
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
                var assemblyPath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
#if DEBUG
                // This lets you edit the files without restarting the server.
                return Path.GetFullPath(Path.Combine(assemblyPath, "..\\..\\html"));
#else
                // This is when you have deployed ythe server.
                return Path.Combine(assemblyPath, "html");
#endif
            }
        }

        /// <summary>
        /// Setups the specified server.
        /// </summary>
        /// <param name="server">The server.</param>
        public static void Setup(WebServer server)
        {
            server.RegisterModule(new StaticFilesModule(HtmlRootPath));
            // The static files module will cache small files in ram until it detects they have been modified.
            server.Module<StaticFilesModule>().UseRamCache = true;
            server.Module<StaticFilesModule>().DefaultExtension = ".html"; 
            // We don't need to add the line below. The default document is always index.html.
            //server.Module<StaticFilesWebModule>().DefaultDocument = "index.html";
        }
    }
}
