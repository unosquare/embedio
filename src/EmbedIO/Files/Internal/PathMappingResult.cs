namespace EmbedIO.Files.Internal
{
    internal enum PathMappingResult
    {
        /// <summary>
        /// The path was not found.
        /// </summary>
        NotFound = 0,

        /// <summary>
        /// The path was mapped to a file.
        /// </summary>
        IsFile = 0x1,

        /// <summary>
        /// The path was mapped to a directory.
        /// </summary>
        IsDirectory = 0x2,
    }
}