using System;
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
        /// <para>The scheme of <see cref="NullUri"/>.</para>
        /// </summary>
        public const string UriSchemeNull = "null";

        /// <summary>
        /// The signature string included in <c>Server</c> response headers.
        /// </summary>
        public static readonly string Signature = "EmbedIO/" + Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyInformationalVersionAttribute>().First().InformationalVersion;

        /// <summary>
        /// <para>An <see cref="Encoding"/> that can be used to send UTF-8 responses without a byte order mark (BOM).</para>
        /// <para>This is the default encoding used by <see cref="WebServer"/> and should be used instead of <see cref="Encoding.UTF8"/>
        /// when specifying an encoding for <see cref="HttpContextExtensions.OpenResponseText"/>.</para>
        /// </summary>
        public static readonly Encoding Utf8NoBomEncoding = new UTF8Encoding(false);

        /// <summary>
        /// <para>The default encoding that is both assumed for requests that do not specify an encoding,
        /// and used for responses when an encoding is not specified.</para>
        /// <para>This is the same as <see cref="Utf8NoBomEncoding"/>.</para>
        /// </summary>
        public static readonly Encoding DefaultEncoding = Utf8NoBomEncoding;

        /// <summary>
        /// <para>An <see cref="Uri"/> which cannot be equal to any HTTP / HTTP URI.</para>
        /// <para>Used as the default value for non-nullable properties of type <see cref="Uri"/>.</para>
        /// </summary>
        public static readonly Uri NullUri = new ("null:");
    }
}