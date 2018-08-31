namespace Unosquare.Net
{
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Text;
    using System;
    using Labs.EmbedIO;
    using Labs.EmbedIO.Constants;
    using Swan;

    /// <summary>
    /// Represents some System.NET custom extensions.
    /// </summary>
    internal static class NetExtensions
    {
        internal const string Tspecials = "()<>@,;:\\\"/[]?={} \t";

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
        
        internal static byte[] ToHostOrder(this byte[] source, Endianness sourceOrder)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Length > 1 && !sourceOrder.IsHostOrder() ? source.Reverse().ToArray() : source;
        }
        
        internal static bool IsHostOrder(this Endianness order)
        {
            // true: !(true ^ true) or !(false ^ false)
            // false: !(true ^ false) or !(false ^ true)
            return !(BitConverter.IsLittleEndian ^ (order == Endianness.Little));
        }
        
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
                message = $"Not an absolute URI: {uriString}";
                return false;
            }

            var schm = uri.Scheme;
            if (!(schm == "ws" || schm == "wss"))
            {
                message = $"The scheme part isn\'t \'ws\' or \'wss\': {uriString}";
                return false;
            }

            if (uri.Fragment.Length > 0)
            {
                message = $"Includes the fragment component: {uriString}";
                return false;
            }

            var port = uri.Port;
            if (port == 0)
            {
                message = $"The port part is zero: {uriString}";
                return false;
            }

            result = port != -1
                ? uri
                : new Uri(
                    $"{schm}://{uri.Host}:{(schm == "ws" ? 80 : 443)}{uri.PathAndQuery}");

            message = string.Empty;
            return true;
        }

        internal static bool IsToken(this string value) =>
            value.All(c => c >= 0x20 && c < 0x7f && !Tspecials.Contains(c));
        
        internal static CookieCollection GetCookies(this NameValueCollection headers, bool response)
        {
            var name = response ? "Set-Cookie" : Headers.Cookie;
            return headers != null && headers.AllKeys.Contains(name)
                ? CookieCollection.Parse(headers[name], response)
                : new CookieCollection();
        }

        internal static string ToExtensionString(this CompressionMethod method, params string[] parameters)
        {
            if (method == CompressionMethod.None)
                return string.Empty;

            var m = $"permessage-{method.ToString().ToLower()}";

            return parameters == null || parameters.Length == 0 ? m : $"{m}; {string.Join("; ", parameters)}";
        }
        
        internal static bool Contains(this NameValueCollection collection, string name, string value)
            => collection[name]?.Split(Strings.CommaSplitChar)
                   .Any(val => val.Trim().Equals(value, StringComparison.OrdinalIgnoreCase)) == true;
        
        internal static bool Contains(this string value, params char[] chars)
            => chars?.Length == 0 || (!string.IsNullOrEmpty(value) && value.IndexOfAny(chars) > -1);

        internal static bool IsCompressionExtension(this string value, CompressionMethod method) =>
            value.StartsWith(method.ToExtensionString());
    }
}