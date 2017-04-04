#if !NET46
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO;
using Unosquare.Swan;

namespace Unosquare.Net
{
    /// <summary>
    /// Indicates the status code for the WebSocket connection close.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   The values of this enumeration are defined in
    ///   <see href="http://tools.ietf.org/html/rfc6455#section-7.4">Section 7.4</see> of RFC 6455.
    ///   </para>
    ///   <para>
    ///   "Reserved value" must not be set as a status code in a connection close frame by
    ///   an endpoint. It's designated for use in applications expecting a status code to
    ///   indicate that the connection was closed due to the system grounds.
    ///   </para>
    /// </remarks>
    public enum CloseStatusCode : ushort
    {
        /// <summary>
        /// Equivalent to close status 1000. Indicates normal close.
        /// </summary>
        Normal = 1000,
        /// <summary>
        /// Equivalent to close status 1001. Indicates that an endpoint is going away.
        /// </summary>
        Away = 1001,
        /// <summary>
        /// Equivalent to close status 1002. Indicates that an endpoint is terminating
        /// the connection due to a protocol error.
        /// </summary>
        ProtocolError = 1002,
        /// <summary>
        /// Equivalent to close status 1003. Indicates that an endpoint is terminating
        /// the connection because it has received a type of data that it cannot accept.
        /// </summary>
        UnsupportedData = 1003,
        /// <summary>
        /// Equivalent to close status 1004. Still undefined. A Reserved value.
        /// </summary>
        Undefined = 1004,
        /// <summary>
        /// Equivalent to close status 1005. Indicates that no status code was actually present.
        /// A Reserved value.
        /// </summary>
        NoStatus = 1005,
        /// <summary>
        /// Equivalent to close status 1006. Indicates that the connection was closed abnormally.
        /// A Reserved value.
        /// </summary>
        Abnormal = 1006,
        /// <summary>
        /// Equivalent to close status 1007. Indicates that an endpoint is terminating
        /// the connection because it has received a message that contains data that
        /// isn't consistent with the type of the message.
        /// </summary>
        InvalidData = 1007,
        /// <summary>
        /// Equivalent to close status 1008. Indicates that an endpoint is terminating
        /// the connection because it has received a message that violates its policy.
        /// </summary>
        PolicyViolation = 1008,
        /// <summary>
        /// Equivalent to close status 1009. Indicates that an endpoint is terminating
        /// the connection because it has received a message that is too big to process.
        /// </summary>
        TooBig = 1009,
        /// <summary>
        /// Equivalent to close status 1010. Indicates that a client is terminating
        /// the connection because it has expected the server to negotiate one or more extension,
        /// but the server didn't return them in the handshake response.
        /// </summary>
        MandatoryExtension = 1010,
        /// <summary>
        /// Equivalent to close status 1011. Indicates that a server is terminating
        /// the connection because it has encountered an unexpected condition that
        /// prevented it from fulfilling the request.
        /// </summary>
        ServerError = 1011,
        /// <summary>
        /// Equivalent to close status 1015. Indicates that the connection was closed
        /// due to a failure to perform a TLS handshake. A Reserved value.
        /// </summary>
        TlsHandshakeFailure = 1015
    }

    /// <summary>
    /// Represents some System.NET custom extensions
    /// </summary>
    public static class WebSocketExtensions
    {
        internal static IEnumerable<string> SplitHeaderValue(this string value, params char[] separators)
        {
            var len = value.Length;
            var seps = new string(separators);

            var buff = new StringBuilder(32);
            var escaped = false;
            var quoted = false;

            for (var i = 0; i < len; i++)
            {
                var c = value[i];
                if (c == '"')
                {
                    if (escaped)
                        escaped = false;
                    else
                        quoted = !quoted;
                }
                else if (c == '\\')
                {
                    if (i < len - 1 && value[i + 1] == '"')
                        escaped = true;
                }
                else if (seps.Contains(c))
                {
                    if (!quoted)
                    {
                        yield return buff.ToString();
                        buff.Length = 0;

                        continue;
                    }
                }

                buff.Append(c);
            }

            if (buff.Length > 0)
                yield return buff.ToString();
        }

        internal static string Unquote(this string str)
        {
            var start = str.IndexOf('\"');
            var end = str.LastIndexOf('\"');

            if (start >= 0 && end >= 0)
                str = str.Substring(start + 1, end - 1);
            return str.Trim();
        }

        /// <summary>
        /// Retrieves a sub-array from the specified <paramref name="array"/>. A sub-array starts at
        /// the specified element position in <paramref name="array"/>.
        /// </summary>
        /// <returns>
        /// An array of T that receives a sub-array, or an empty array of T if any problems with
        /// the parameters.
        /// </returns>
        /// <param name="array">
        /// An array of T from which to retrieve a sub-array.
        /// </param>
        /// <param name="startIndex">
        /// An <see cref="int"/> that represents the zero-based starting position of
        /// a sub-array in <paramref name="array"/>.
        /// </param>
        /// <param name="length">
        /// An <see cref="int"/> that represents the number of elements to retrieve.
        /// </param>
        /// <typeparam name="T">
        /// The type of elements in <paramref name="array"/>.
        /// </typeparam>
        public static T[] SubArray<T>(this T[] array, int startIndex, int length)
        {
            int len;
            if (array == null || (len = array.Length) == 0)
                return new T[0];

            if (startIndex < 0 || length <= 0 || startIndex + length > len)
                return new T[0];

            if (startIndex == 0 && length == len)
                return array;

            var subArray = new T[length];
            Array.Copy(array, startIndex, subArray, 0, length);

            return subArray;
        }

        /// <summary>
        /// Retrieves a sub-array from the specified <paramref name="array"/>. A sub-array starts at
        /// the specified element position in <paramref name="array"/>.
        /// </summary>
        /// <returns>
        /// An array of T that receives a sub-array, or an empty array of T if any problems with
        /// the parameters.
        /// </returns>
        /// <param name="array">
        /// An array of T from which to retrieve a sub-array.
        /// </param>
        /// <param name="startIndex">
        /// A <see cref="long"/> that represents the zero-based starting position of
        /// a sub-array in <paramref name="array"/>.
        /// </param>
        /// <param name="length">
        /// A <see cref="long"/> that represents the number of elements to retrieve.
        /// </param>
        /// <typeparam name="T">
        /// The type of elements in <paramref name="array"/>.
        /// </typeparam>
        public static T[] SubArray<T>(this T[] array, long startIndex, long length)
        {
            return array.SubArray((int)startIndex, (int)length);
        }

        // TODO: Remove SubArray with new SWAN

        internal static bool IsData(this byte opcode)
        {
            return opcode == 0x1 || opcode == 0x2;
        }

        internal static bool IsData(this Opcode opcode)
        {
            return opcode == Opcode.Text || opcode == Opcode.Binary;
        }

        internal static byte[] InternalToByteArray(this ushort value, Endianness order)
        {
            var bytes = BitConverter.GetBytes(value);
            if (!order.IsHostOrder())
                Array.Reverse(bytes);

            return bytes;
        }

        internal static byte[] InternalToByteArray(this ulong value, Endianness order)
        {
            var bytes = BitConverter.GetBytes(value);
            if (!order.IsHostOrder())
                Array.Reverse(bytes);

            return bytes;
        }

        internal static bool IsControl(this byte opcode)
        {
            return opcode > 0x7 && opcode < 0x10;
        }

        internal static async Task<byte[]> ReadBytesAsync(this Stream stream, long length, int bufferLength, CancellationToken ct = default(CancellationToken))
        {
            using (var dest = new MemoryStream())
            {
                try
                {
                    var buff = new byte[bufferLength];
                    while (length > 0)
                    {
                        if (length < bufferLength)
                            bufferLength = (int)length;

                        var nread = await stream.ReadAsync(buff, 0, bufferLength, ct);
                        if (nread == 0)
                            break;

                        dest.Write(buff, 0, nread);
                        length -= nread;
                    }
                }
                catch
                {
                    // ignored
                }

                return dest.ToArray();
            }
        }

        internal static async Task<byte[]> ReadBytesAsync(this Stream stream, int length, CancellationToken ct = default(CancellationToken))
        {
            var buff = new byte[length];
            var offset = 0;
            try
            {
                while (length > 0)
                {
                    var nread = await stream.ReadAsync(buff, offset, length, ct);
                    if (nread == 0)
                        break;

                    offset += nread;
                    length -= nread;
                }
            }
            catch
            {
                // ignored
            }

            return buff.SubArray(0, offset);
        }
        
        internal static bool IsReserved(this CloseStatusCode code)
        {
            return code == CloseStatusCode.Undefined ||
                   code == CloseStatusCode.NoStatus ||
                   code == CloseStatusCode.Abnormal ||
                   code == CloseStatusCode.TlsHandshakeFailure;
        }

        /// <summary>
        /// Converts the order of the specified array of <see cref="byte"/> to the host byte order.
        /// </summary>
        /// <returns>
        /// An array of <see cref="byte"/> converted from <paramref name="source"/>.
        /// </returns>
        /// <param name="source">
        /// An array of <see cref="byte"/> to convert.
        /// </param>
        /// <param name="sourceOrder">
        /// One of the <see cref="Endianness"/> enum values, specifies the byte order of
        /// <paramref name="source"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/> is <see langword="null"/>.
        /// </exception>
        public static byte[] ToHostOrder(this byte[] source, Endianness sourceOrder)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Length > 1 && !sourceOrder.IsHostOrder() ? source.Reverse().ToArray() : source;
        }

        /// <summary>
        /// Determines whether the specified <see cref="Endianness"/> is host (this computer
        /// architecture) byte order.
        /// </summary>
        /// <returns>
        /// <c>true</c> if <paramref name="order"/> is host byte order; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="order">
        /// One of the <see cref="Endianness"/> enum values, to test.
        /// </param>
        internal static bool IsHostOrder(this Endianness order)
        {
            // true: !(true ^ true) or !(false ^ false)
            // false: !(true ^ false) or !(false ^ true)
            return !(BitConverter.IsLittleEndian ^ (order == Endianness.Little));
        }

        /// <summary>
        /// Determines whether the specified <see cref="string"/> is a predefined scheme.
        /// </summary>
        /// <returns>
        /// <c>true</c> if <paramref name="value"/> is a predefined scheme; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="value">
        /// A <see cref="string"/> to test.
        /// </param>
        internal static bool IsPredefinedScheme(this string value)
        {
            if (value == null || value.Length < 2)
                return false;

            var c = value[0];

            switch (c)
            {
                case 'h':
                    return value == "http" || value == "https";
                case 'w':
                    return value == "ws" || value == "wss";
                case 'f':
                    return value == "file" || value == "ftp";
                case 'n':
                    c = value[1];
                    return c == 'e'
                        ? value == "news" || value == "net.pipe" || value == "net.tcp"
                        : value == "nntp";
                default:
                    return (c == 'g' && value == "gopher") || (c == 'm' && value == "mailto");
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="string"/> is a URI string.
        /// </summary>
        /// <returns>
        /// <c>true</c> if <paramref name="value"/> may be a URI string; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="value">
        /// A <see cref="string"/> to test.
        /// </param>
        internal static bool MaybeUri(this string value)
        {
            var idx = value?.IndexOf(':');

            if (idx.HasValue == false || idx == -1)
                return false;

            return idx < 10 && value.Substring(0, idx.Value).IsPredefinedScheme();
        }

        /// <summary>
        /// Converts the specified <see cref="string"/> to a <see cref="Uri"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="Uri"/> converted from <paramref name="uriString"/>,
        /// or <see langword="null"/> if <paramref name="uriString"/> isn't successfully converted.
        /// </returns>
        /// <param name="uriString">
        /// A <see cref="string"/> to convert.
        /// </param>
        internal static Uri ToUri(this string uriString)
        {
            Uri ret;
            Uri.TryCreate(
              uriString, uriString.MaybeUri() ? UriKind.Absolute : UriKind.Relative, out ret);

            return ret;
        }

        /// <summary>
        /// Tries to create a <see cref="Uri"/> for WebSocket with
        /// the specified <paramref name="uriString"/>.
        /// </summary>
        /// <returns>
        /// <c>true</c> if a <see cref="Uri"/> is successfully created; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="uriString">
        /// A <see cref="string"/> that represents a WebSocket URL to try.
        /// </param>
        /// <param name="result">
        /// When this method returns, a <see cref="Uri"/> that represents a WebSocket URL,
        /// or <see langword="null"/> if <paramref name="uriString"/> is invalid.
        /// </param>
        /// <param name="message">
        /// When this method returns, a <see cref="string"/> that represents an error message,
        /// or <see cref="String.Empty"/> if <paramref name="uriString"/> is valid.
        /// </param>
        internal static bool TryCreateWebSocketUri(
          this string uriString, out Uri result, out string message)
        {
            result = null;

            var uri = uriString.ToUri();
            if (uri == null)
            {
                message = "An invalid URI string: " + uriString;
                return false;
            }

            if (!uri.IsAbsoluteUri)
            {
                message = "Not an absolute URI: " + uriString;
                return false;
            }

            var schm = uri.Scheme;
            if (!(schm == "ws" || schm == "wss"))
            {
                message = "The scheme part isn't 'ws' or 'wss': " + uriString;
                return false;
            }

            if (uri.Fragment.Length > 0)
            {
                message = "Includes the fragment component: " + uriString;
                return false;
            }

            var port = uri.Port;
            if (port == 0)
            {
                message = "The port part is zero: " + uriString;
                return false;
            }

            result = port != -1
                     ? uri
                     : new Uri(
                    $"{schm}://{uri.Host}:{(schm == "ws" ? 80 : 443)}{uri.PathAndQuery}");

            message = string.Empty;
            return true;
        }

        private const string Tspecials = "()<>@,;:\\\"/[]?={} \t";

        internal static bool IsToken(this string value)
        {
            return value.All(c => c >= 0x20 && c < 0x7f && !Tspecials.Contains(c));
        }

        /// <summary>
        /// Gets the collection of the HTTP cookies from the specified HTTP <paramref name="headers"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="CookieCollection"/> that receives a collection of the HTTP cookies.
        /// </returns>
        /// <param name="headers">
        /// A <see cref="NameValueCollection"/> that contains a collection of the HTTP headers.
        /// </param>
        /// <param name="response">
        /// <c>true</c> if <paramref name="headers"/> is a collection of the response headers;
        /// otherwise, <c>false</c>.
        /// </param>
        public static CookieCollection GetCookies(this NameValueCollection headers, bool response)
        {
            var name = response ? "Set-Cookie" : "Cookie";
            return headers != null && headers.AllKeys.Contains(name)
                   ? CookieCollection.Parse(headers[name], response)
                   : new CookieCollection();
        }

        internal static string ToExtensionString(this CompressionMethod method, params string[] parameters)
        {
            if (method == CompressionMethod.None)
                return string.Empty;

            var m = $"permessage-{method.ToString().ToLower()}";

            if (parameters == null || parameters.Length == 0)
                return m;

            return $"{m}; {string.Join("; ", parameters)}";
        }

        /// <summary>
        /// Determines whether the specified <see cref="NameValueCollection"/> contains the entry with
        /// the specified both <paramref name="name"/> and <paramref name="value"/>.
        /// </summary>
        /// <returns>
        /// <c>true</c> if <paramref name="collection"/> contains the entry with both
        /// <paramref name="name"/> and <paramref name="value"/>; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="collection">
        /// A <see cref="NameValueCollection"/> to test.
        /// </param>
        /// <param name="name">
        /// A <see cref="string"/> that represents the key of the entry to find.
        /// </param>
        /// <param name="value">
        /// A <see cref="string"/> that represents the value of the entry to find.
        /// </param>
        public static bool Contains(this NameValueCollection collection, string name, string value)
        {
            if (collection == null || collection.Count == 0)
                return false;

            var vals = collection[name];

            return vals != null && vals.Split(',').Any(val => val.Trim().Equals(value, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines whether the specified <see cref="string"/> contains any of characters in
        /// the specified array of <see cref="char"/>.
        /// </summary>
        /// <returns>
        /// <c>true</c> if <paramref name="value"/> contains any of <paramref name="chars"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        /// <param name="value">
        /// A <see cref="string"/> to test.
        /// </param>
        /// <param name="chars">
        /// An array of <see cref="char"/> that contains characters to find.
        /// </param>
        public static bool Contains(this string value, params char[] chars)
        {
            return chars?.Length == 0 || !string.IsNullOrEmpty(value) && value.IndexOfAny(chars) > -1;
        }

        internal static bool IsCompressionExtension(this string value, CompressionMethod method) => value.StartsWith(method.ToExtensionString());
    }
}
#endif