namespace Unosquare.Net
{
    using System;
    using System.Text;
    using System.Security.Cryptography;

    internal class WebSocketKey
    {
        private const string Guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        public string KeyValue { get; set; }

        internal string CreateResponseKey()
        {
            var buff = new StringBuilder(KeyValue, 64);
            buff.Append(Guid);
#pragma warning disable CA5350 // Do Not Use Weak Cryptographic Algorithms
            var sha1 = SHA1.Create();
#pragma warning restore CA5350 // Do Not Use Weak Cryptographic Algorithms
            var src = sha1.ComputeHash(Encoding.UTF8.GetBytes(buff.ToString()));

            return Convert.ToBase64String(src);
        }
    }
}
