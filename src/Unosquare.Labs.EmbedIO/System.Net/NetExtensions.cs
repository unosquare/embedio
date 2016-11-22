#if !NET46
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System;

namespace Unosquare.Net
{
    /// <summary>
    /// Specifies the byte order.
    /// </summary>
    public enum ByteOrder
    {
        /// <summary>
        /// Specifies Little-endian.
        /// </summary>
        Little,
        /// <summary>
        /// Specifies Big-endian.
        /// </summary>
        Big
    }

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
    public static class NetExtensions
    {
        #region WebSocket

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

        internal static string Unquote(this string value)
        {
            var start = value.IndexOf('"');
            if (start < 0)
                return value;

            var end = value.LastIndexOf('"');
            var len = end - start - 1;

            return len < 0
                   ? value
                   : len == 0
                     ? string.Empty
                     : value.Substring(start + 1, len).Replace("\\\"", "\"");
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

        internal static bool IsData(this byte opcode)
        {
            return opcode == 0x1 || opcode == 0x2;
        }

        internal static bool IsData(this Opcode opcode)
        {
            return opcode == Opcode.Text || opcode == Opcode.Binary;
        }

        internal static byte[] InternalToByteArray(this ushort value, ByteOrder order)
        {
            var bytes = BitConverter.GetBytes(value);
            if (!order.IsHostOrder())
                Array.Reverse(bytes);

            return bytes;
        }

        internal static byte[] InternalToByteArray(this ulong value, ByteOrder order)
        {
            var bytes = BitConverter.GetBytes(value);
            if (!order.IsHostOrder())
                Array.Reverse(bytes);

            return bytes;
        }

        internal static byte[] Append(this ushort code, string reason)
        {
            var ret = code.InternalToByteArray(ByteOrder.Big);
            if (string.IsNullOrEmpty(reason)) return ret;

            var buff = new List<byte>(ret);
            buff.AddRange(Encoding.UTF8.GetBytes(reason));
            ret = buff.ToArray();

            return ret;
        }

        internal static bool IsControl(this byte opcode)
        {
            return opcode > 0x7 && opcode < 0x10;
        }

        internal static byte[] ReadBytes(this Stream stream, long length, int bufferLength)
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

                        var nread = stream.Read(buff, 0, bufferLength);
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

#if NET452
                dest.Close();
#endif
                return dest.ToArray();
            }
        }

        internal static void WriteBytes(this Stream stream, byte[] bytes, int bufferLength)
        {
            using (var input = new MemoryStream(bytes))
                input.CopyTo(stream, bufferLength);
        }

        internal static byte[] ReadBytes(this Stream stream, int length)
        {
            var buff = new byte[length];
            var offset = 0;
            try
            {
                while (length > 0)
                {
                    var nread = stream.Read(buff, offset, length);
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

        private static readonly int _retry = 5;

        internal static void ReadBytesAsync(
          this Stream stream, int length, Action<byte[]> completed, Action<Exception> error
        )
        {
            var buff = new byte[length];
            var offset = 0;
            var retry = 0;

            AsyncCallback callback = null;
            callback =
              ar =>
              {
                  try
                  {
                      var nread = stream.EndRead(ar);
                      if (nread == 0 && retry < _retry)
                      {
                          retry++;
                          stream.BeginRead(buff, offset, length, callback, null);

                          return;
                      }

                      if (nread == 0 || nread == length)
                      {
                          if (completed != null)
                              completed(buff.SubArray(0, offset + nread));

                          return;
                      }

                      retry = 0;

                      offset += nread;
                      length -= nread;

                      stream.BeginRead(buff, offset, length, callback, null);
                  }
                  catch (Exception ex)
                  {
                      if (error != null)
                          error(ex);
                  }
              };

            try
            {
                stream.BeginRead(buff, offset, length, callback, null);
            }
            catch (Exception ex)
            {
                if (error != null)
                    error(ex);
            }
        }

        internal static void ReadBytesAsync(
          this Stream stream,
          long length,
          int bufferLength,
          Action<byte[]> completed,
          Action<Exception> error
        )
        {
            var dest = new MemoryStream();
            var buff = new byte[bufferLength];
            var retry = 0;

            Action<long> read = null;
            read =
              len =>
              {
                  if (len < bufferLength)
                      bufferLength = (int)len;

                  stream.BeginRead(
              buff,
              0,
              bufferLength,
              ar =>
              {
                  try
                  {
                      var nread = stream.EndRead(ar);
                      if (nread > 0)
                          dest.Write(buff, 0, nread);

                      if (nread == 0 && retry < _retry)
                      {
                          retry++;
                          read(len);

                          return;
                      }

                      if (nread == 0 || nread == len)
                      {
                          if (completed != null)
                          {
#if NET452
                              dest.Close();
#endif
                              completed(dest.ToArray());
                          }

                          dest.Dispose();
                          return;
                      }

                      retry = 0;
                      read(len - nread);
                  }
                  catch (Exception ex)
                  {
                      dest.Dispose();
                      error?.Invoke(ex);
                  }
              },
              null
            );
              };

            try
            {
                read(length);
            }
            catch (Exception ex)
            {
                dest.Dispose();
                error?.Invoke(ex);
            }
        }

        internal static bool IsSupported(this byte opcode)
        {
            return Enum.IsDefined(typeof(Opcode), opcode);
        }

        internal static ulong ToUInt64(this byte[] source, ByteOrder sourceOrder)
        {
            return BitConverter.ToUInt64(source.ToHostOrder(sourceOrder), 0);
        }

        internal static string UTF8Decode(this byte[] bytes)
        {
            try
            {
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return null;
            }
        }

        internal static bool IsReserved(this ushort code)
        {
            return code == (ushort)CloseStatusCode.Undefined ||
                   code == (ushort)CloseStatusCode.NoStatus ||
                   code == (ushort)CloseStatusCode.Abnormal ||
                   code == (ushort)CloseStatusCode.TlsHandshakeFailure;
        }

        internal static bool IsReserved(this CloseStatusCode code)
        {
            return code == CloseStatusCode.Undefined ||
                   code == CloseStatusCode.NoStatus ||
                   code == CloseStatusCode.Abnormal ||
                   code == CloseStatusCode.TlsHandshakeFailure;
        }

        internal static ushort ToUInt16(this byte[] source, ByteOrder sourceOrder)
        {
            return BitConverter.ToUInt16(source.ToHostOrder(sourceOrder), 0);
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
        /// One of the <see cref="ByteOrder"/> enum values, specifies the byte order of
        /// <paramref name="source"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/> is <see langword="null"/>.
        /// </exception>
        public static byte[] ToHostOrder(this byte[] source, ByteOrder sourceOrder)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Length > 1 && !sourceOrder.IsHostOrder() ? source.Reverse().ToArray() : source;
        }

        /// <summary>
        /// Determines whether the specified <see cref="ByteOrder"/> is host (this computer
        /// architecture) byte order.
        /// </summary>
        /// <returns>
        /// <c>true</c> if <paramref name="order"/> is host byte order; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="order">
        /// One of the <see cref="ByteOrder"/> enum values, to test.
        /// </param>
        public static bool IsHostOrder(this ByteOrder order)
        {
            // true: !(true ^ true) or !(false ^ false)
            // false: !(true ^ false) or !(false ^ true)
            return !(BitConverter.IsLittleEndian ^ (order == ByteOrder.Little));
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
        public static bool IsPredefinedScheme(this string value)
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
        public static bool MaybeUri(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            var idx = value.IndexOf(':');
            if (idx == -1)
                return false;

            return idx < 10 && value.Substring(0, idx).IsPredefinedScheme();
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
        public static Uri ToUri(this string uriString)
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

        internal static bool ContainsTwice(string[] values)
        {
            var len = values.Length;

            Func<int, bool> contains = null;
            contains = idx =>
            {
                if (idx >= len - 1) return false;

                for (var i = idx + 1; i < len; i++)
                    if (values[i] == values[idx])
                        return true;

                return contains(++idx);
            };

            return contains(0);
        }

        internal static string CheckIfValidProtocols(this string[] protocols)
        {
            return protocols.Any(protocol => string.IsNullOrEmpty(protocol) || !protocol.IsToken())
                   ? "Contains an invalid value."
                   : ContainsTwice(protocols)
                     ? "Contains a value twice."
                     : null;
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

        internal static bool CheckWaitTime(this TimeSpan time, out string message)
        {
            message = null;

            if (time > TimeSpan.Zero) return true;

            message = "A wait time is zero or less.";
            return false;
        }

        /// <summary>
        /// Converts the specified <paramref name="array"/> to a <see cref="string"/> that
        /// concatenates the each element of <paramref name="array"/> across the specified
        /// <paramref name="separator"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> converted from <paramref name="array"/>,
        /// or <see cref="String.Empty"/> if <paramref name="array"/> is empty.
        /// </returns>
        /// <param name="array">
        /// An array of T to convert.
        /// </param>
        /// <param name="separator">
        /// A <see cref="string"/> that represents the separator string.
        /// </param>
        /// <typeparam name="T">
        /// The type of elements in <paramref name="array"/>.
        /// </typeparam>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="array"/> is <see langword="null"/>.
        /// </exception>
        public static string ToString<T>(this T[] array, string separator)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            var len = array.Length;
            if (len == 0)
                return string.Empty;

            if (separator == null)
                separator = string.Empty;

            var buff = new StringBuilder(64);
            (len - 1).Times(i => buff.AppendFormat("{0}{1}", array[i].ToString(), separator));

            buff.Append(array[len - 1]);
            return buff.ToString();
        }

        /// <summary>
        /// Executes the specified <c>Action&lt;int&gt;</c> delegate <paramref name="n"/> times.
        /// </summary>
        /// <param name="n">
        /// An <see cref="int"/> is the number of times to execute.
        /// </param>
        /// <param name="action">
        /// An <c>Action&lt;int&gt;</c> delegate that references the method(s) to execute.
        /// An <see cref="int"/> parameter to pass to the method(s) is the zero-based count of
        /// iteration.
        /// </param>
        public static void Times(this int n, Action<int> action)
        {
            if (n <= 0 || action == null) return;
            for (var i = 0; i < n; i++)
                action(i);
        }

        internal static string ToExtensionString(this CompressionMethod method, params string[] parameters)
        {
            if (method == CompressionMethod.None)
                return string.Empty;

            var m = $"permessage-{method.ToString().ToLower()}";

            if (parameters == null || parameters.Length == 0)
                return m;

            return $"{m}; {parameters.ToString("; ")}";
        }

        internal static bool IsText(this string value)
        {
            var len = value.Length;
            for (var i = 0; i < len; i++)
            {
                var c = value[i];
                if (c < 0x20 && !"\r\n\t".Contains(c))
                    return false;

                if (c == 0x7f)
                    return false;

                if (c == '\n' && ++i < len)
                {
                    c = value[i];
                    if (!" \t".Contains(c))
                        return false;
                }
            }

            return true;
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
            return chars == null || chars.Length == 0 || !string.IsNullOrEmpty(value) && value.IndexOfAny(chars) > -1;
        }

        private static byte[] decompress(this byte[] data)
        {
            if (data.Length == 0)
                return data;

            using (var input = new MemoryStream(data))
                return input.decompressToArray();
        }

        private static MemoryStream decompress(this Stream stream)
        {
            var output = new MemoryStream();
            if (stream.Length == 0)
                return output;

            stream.Position = 0;
            using (var ds = new DeflateStream(stream, CompressionMode.Decompress, true))
            {
                ds.CopyTo(output, 1024);
                output.Position = 0;

                return output;
            }
        }

        private static byte[] compress(this byte[] data)
        {
            if (data.Length == 0)
                //return new byte[] { 0x00, 0x00, 0x00, 0xff, 0xff };
                return data;

            using (var input = new MemoryStream(data))
                return input.compressToArray();
        }

        private static readonly byte[] Last = new byte[] { 0x00 };

        private static MemoryStream compress(this Stream stream)
        {
            var output = new MemoryStream();
            if (stream.Length == 0)
                return output;

            stream.Position = 0;
            using (var ds = new DeflateStream(output, CompressionMode.Compress, true))
            {
                stream.CopyTo(ds, 1024);
#if NET452
                ds.Close(); // BFINAL set to 1.
#endif
                output.Write(Last, 0, 1);
                output.Position = 0;

                return output;
            }
        }

        internal static Stream Compress(this Stream stream, CompressionMethod method)
        {
            return method == CompressionMethod.Deflate
                   ? stream.compress()
                   : stream;
        }

        private static byte[] compressToArray(this Stream stream)
        {
            using (var output = stream.compress())
            {
#if NET452
                output.Close();
#endif
                return output.ToArray();
            }
        }
        internal static byte[] Compress(this byte[] data, CompressionMethod method)
        {
            return method == CompressionMethod.Deflate
                   ? data.compress()
                   : data;
        }

        private static byte[] decompressToArray(this Stream stream)
        {
            using (var output = stream.decompress())
            {
#if NET452
                output.Close();
#endif
                return output.ToArray();
            }
        }

        internal static byte[] Decompress(this byte[] data, CompressionMethod method)
        {
            return method == CompressionMethod.Deflate
                   ? data.decompress()
                   : data;
        }

        internal static Stream Decompress(this Stream stream, CompressionMethod method)
        {
            return method == CompressionMethod.Deflate
                   ? stream.decompress()
                   : stream;
        }
        internal static bool IsCompressionExtension(this string value, CompressionMethod method)
        {
            return value.StartsWith(method.ToExtensionString());
        }

        internal static byte[] ToByteArray(this Stream stream)
        {
            using (var output = new MemoryStream())
            {
                stream.Position = 0;
                stream.CopyTo(output, 1024);
#if NET452
                output.Close();
#endif
                return output.ToArray();
            }
        }

        internal static byte[] DecompressToArray(this Stream stream, CompressionMethod method)
        {
            return method == CompressionMethod.Deflate
                   ? stream.decompressToArray()
                   : stream.ToByteArray();
        }

        #endregion
    }
}
#endif