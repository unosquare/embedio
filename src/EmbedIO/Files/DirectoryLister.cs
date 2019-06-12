using EmbedIO.Files.Internal;

namespace EmbedIO.Files
{
    public static class DirectoryLister
    {
        public static IDirectoryLister Default => Html;

        public static IDirectoryLister Html => HtmlDirectoryLister.Instance;
    }
}