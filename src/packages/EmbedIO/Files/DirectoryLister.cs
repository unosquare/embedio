using EmbedIO.Files.Internal;

namespace EmbedIO.Files
{
    /// <summary>
    /// Provides standard directory listers for <see cref="FileModule"/>.
    /// </summary>
    /// <seealso cref="IDirectoryLister"/>
    public static class DirectoryLister
    {
        /// <summary>
        /// <para>Gets an <see cref="IDirectoryLister"/> interface
        /// that produces a HTML listing of a directory.</para>
        /// <para>The output of the returned directory lister
        /// is the same as a directory listing obtained
        /// by EmbedIO version 2.</para>
        /// </summary>
        public static IDirectoryLister Html => HtmlDirectoryLister.Instance;
    }
}