using EmbedIO.Files.Internal;

namespace EmbedIO.Files
{
    /// <summary>
    /// Static class to common Directory Listers.
    /// </summary>
    public static class DirectoryLister
    {
        /// <summary>
        /// Gets the Directory Lister with HTML output.
        /// </summary>
        public static IDirectoryLister Html => HtmlDirectoryLister.Instance;
    }
}