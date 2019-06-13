using EmbedIO.Files.Internal;

namespace EmbedIO.Files
{
    public static class DirectoryLister
    {
        public static IDirectoryLister Html => HtmlDirectoryLister.Instance;
    }
}