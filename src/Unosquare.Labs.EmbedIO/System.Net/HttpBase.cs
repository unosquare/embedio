namespace Unosquare.Net
{
    using System;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Text;

    internal abstract class HttpBase
    {
        protected HttpBase(Version version, NameValueCollection headers)
        {
            ProtocolVersion = version;
            Headers = headers;
        }

        public NameValueCollection Headers { get; }

        public Version ProtocolVersion { get; }

        public byte[] ToByteArray() => Encoding.UTF8.GetBytes(ToString());

        internal static string GetValue(string nameAndValue)
        {
            var idx = nameAndValue.IndexOf('=');

            return idx < 0 || idx == nameAndValue.Length - 1 ? null : nameAndValue.Substring(idx + 1).Trim().Unquote();
        }

        internal static Encoding GetEncoding(string contentType) => contentType
            .Split(';')
            .Select(p => p.Trim())
            .Where(part => part.StartsWith("charset", StringComparison.OrdinalIgnoreCase))
            .Select(part => Encoding.GetEncoding(GetValue(part)))
            .FirstOrDefault();

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
    }
}