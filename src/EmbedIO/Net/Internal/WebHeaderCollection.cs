using System;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace EmbedIO.Net.Internal
{
    internal class WebHeaderCollection : NameValueCollection
    {
        public override string ToString()
        {
            var buff = new StringBuilder();

            foreach (string key in Keys)
                buff.AppendFormat("{0}: {1}\r\n", key, Get(key));

            return buff.Append("\r\n").ToString();
        }

        public override void Add(string name, string value) => base.Add(name, CheckValue(value));

        internal static bool IsHeaderValue(string value)
        {
            var len = value.Length;
            for (var i = 0; i < len; i++)
            {
                var c = value[i];
                if (c < 0x20 && !"\r\n\t".Contains(c))
                    return false;

                if (c == 0x7f)
                    return false;

                if (c != '\n' || ++i >= len) continue;

                c = value[i];
                if (!" \t".Contains(c))
                    return false;
            }

            return true;
        }

        private static string CheckValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var trimValue = value.Trim();

            if (trimValue.Length > 65535)
                throw new ArgumentOutOfRangeException(nameof(value), "Greater than 65,535 characters.");

            if (!IsHeaderValue(trimValue))
                throw new ArgumentException("Contains invalid characters.", nameof(value));

            return trimValue;
        }
    }
}