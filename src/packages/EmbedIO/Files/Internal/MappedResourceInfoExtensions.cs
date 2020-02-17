namespace EmbedIO.Files.Internal
{
    internal static class MappedResourceInfoExtensions
    {
        public static string GetEntityTag(this MappedResourceInfo @this, CompressionMethod compressionMethod)
            => EntityTag.Compute(@this.LastModifiedUtc, @this.Length, compressionMethod);
    }
}