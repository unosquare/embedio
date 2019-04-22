namespace Unosquare.Labs.EmbedIO.Core
{
    using System;

    [Flags]
    internal enum PathMappingResult
    {
        /// <summary>
        /// The mask used to extract the mapping result.
        /// </summary>
        MappingMask = 0xF,

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

        /// <summary>
        /// The default extension has been appended to the path.
        /// </summary>
        DefaultExtensionUsed = 0x1000,

        /// <summary>
        /// The default document name has been appended to the path.
        /// </summary>
        DefaultDocumentUsed = 0x2000,
    }
}