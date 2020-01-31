using System.IO;
using System.Linq;
using System.Reflection;

namespace EmbedIO
{
    partial class WebServer
    {
        /// <summary>
        /// <para>The size, in bytes,of buffers used to transfer contents between streams.</para>
        /// <para>The value of this constant is the same as the default used by the
        /// <see cref="Stream.CopyToAsync(Stream)"/> method. For the reasons why this value was chosen, see
        /// <see href="https://referencesource.microsoft.com/#mscorlib/system/io/stream.cs,50">.NET Framework reference source</see>.</para>
        /// </summary>
        public const int StreamCopyBufferSize = 81920;

        /// <summary>
        /// The signature string included in <c>Server</c> response headers.
        /// </summary>
        public static readonly string Signature = "EmbedIO/" + Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyInformationalVersionAttribute>().First().InformationalVersion;
    }
}