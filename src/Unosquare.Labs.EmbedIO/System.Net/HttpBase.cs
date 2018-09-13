namespace Unosquare.Net
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Text;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Swan;

    internal abstract class HttpBase
    {
        protected const string CrLf = "\r\n";

        private const int HeadersMaxLength = 8192;

        private byte[] _entityBodyData;

        protected HttpBase(Version version, NameValueCollection headers)
        {
            ProtocolVersion = version;
            Headers = headers;
        }

        public string EntityBody
        {
            get
            {
                if (_entityBodyData == null || _entityBodyData.Length == 0)
                    return string.Empty;

                Encoding enc = null;

                var contentType = Headers["Content-Type"];
                if (!string.IsNullOrEmpty(contentType))
                    enc = GetEncoding(contentType);

                return (enc ?? Encoding.UTF8).GetString(_entityBodyData);
            }
        }

        public NameValueCollection Headers { get; }

        public Version ProtocolVersion { get; }

        public byte[] ToByteArray() => Encoding.UTF8.GetBytes(ToString());

        internal static string GetValue(string nameAndValue)
        {
            var idx = nameAndValue.IndexOf('=');

            if (idx < 0 || idx == nameAndValue.Length - 1)
                return null;

            return nameAndValue.Substring(idx + 1).Trim().Unquote();
        }

        internal static Encoding GetEncoding(string contentType) => contentType
            .Split(';')
            .Select(p => p.Trim())
            .Where(part => part.StartsWith("charset", StringComparison.OrdinalIgnoreCase))
            .Select(part => Encoding.GetEncoding(GetValue(part))).FirstOrDefault();

        protected static NameValueCollection ParseHeaders(string[] headerParts)
        {
            var headers = new NameValueCollection();
            for (var i = 1; i < headerParts.Length; i++)
            {
                var parts = headerParts[i].Split(':');

                headers[parts[0]] = parts[1];
            }

            return headers;
        }

        protected static async Task<HttpResponse> ReadAsync(
            Stream stream, 
            int millisecondsTimeout = 90000, 
            CancellationToken ct = default)
        {
            var timeout = false;
            var timer = new Timer(
                state =>
                {
                    timeout = true;
#if NET46
                    stream.Close();
#else
                    stream.Dispose();
#endif
                },
                null,
                millisecondsTimeout,
                -1);

            try
            {
                var http = HttpResponse.Parse(ReadHeaders(stream));
                var contentLen = http.Headers["Content-Length"];

                if (!string.IsNullOrEmpty(contentLen))
                    http._entityBodyData = await ReadEntityBodyAsync(stream, contentLen, ct);

                return http;
            }
            catch (Exception ex)
            {
                throw new WebSocketException(timeout
                    ? "A timeout has occurred while reading an HTTP request/response."
                    : "An exception has occurred while reading an HTTP request/response.", ex);
            }
            finally
            {
                timer.Change(-1, -1);
                timer.Dispose();
            }
        }

        private static async Task<byte[]> ReadEntityBodyAsync(Stream stream, string length, CancellationToken ct)
        {
            if (!long.TryParse(length, out var len))
                throw new ArgumentException("Cannot be parsed.", nameof(length));

            if (len < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Less than zero.");

            return len > 1024
                ? await stream.ReadBytesAsync(len, 1024, ct)
                : len > 0
                    ? await stream.ReadBytesAsync((int) len, ct)
                    : null;
        }
        
        private static string[] ReadHeaders(Stream stream)
        {
            var buff = new List<byte>();
            var cnt = 0;
            
            bool EqualsWith(int i, char c)
            {
                if (i == -1)
                    throw new EndOfStreamException("The header cannot be read from the data source.");

                buff.Add((byte) i);
                cnt++;

                return i == c - 0;
            }

            var read = false;
            while (cnt < HeadersMaxLength)
            {
                if (EqualsWith(stream.ReadByte(), '\r') &&
                    EqualsWith(stream.ReadByte(), '\n') &&
                    EqualsWith(stream.ReadByte(), '\r') &&
                    EqualsWith(stream.ReadByte(), '\n'))
                {
                    read = true;
                    break;
                }
            }

            if (!read)
                throw new WebSocketException("The length of header part is greater than the max length.");

            return buff.ToArray()
                .ToText()
                .Replace(CrLf + " ", " ")
                .Replace(CrLf + "\t", " ")
                .Split(new[] {CrLf}, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}