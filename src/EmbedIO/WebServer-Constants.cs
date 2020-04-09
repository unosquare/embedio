using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

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

        /// <summary>
        /// The default encoding that is both assumed for requests that do not specify an encoding,
        /// and used for responses when an encoding is not specified.
        /// </summary>
        public static readonly Encoding DefaultEncoding = Encoding.UTF8;
    }
}