namespace EmbedIO.Files.Internal
{
    internal static class FileCacheItemExtensions
    {
        public static string GetEntityTag(this FileCacheItem @this, CompressionMethod compressionMethod)
            => EntityTag.Compute(@this.LastModifiedUtc, @this.Length, compressionMethod);
    }
}