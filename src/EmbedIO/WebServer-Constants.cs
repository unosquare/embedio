using System.IO;

namespace EmbedIO
{
    partial class WebServer
    {
        /// <summary>
        /// The maximum length, in bytes, for a file or stream to be sent using GZip compression.
        /// </summary>
        public const long MaxGzipLength = (4 * 1024 * 1024) - 1;

        /// <summary>
        /// <para>The size, in bytes,of buffers used to transfer contents between streams.</para>
        /// <para>The value of this constant is the same as the default used by the
        /// <see cref="Stream.CopyToAsync(Stream)"/> method. For the reasons why this value was chosen, see
        /// <see href="https://referencesource.microsoft.com/#mscorlib/system/io/stream.cs,50">.NET Framework reference source</see>.</para>
        /// </summary>
        public const int StreamCopyBufferSize = 81920;
    }
}