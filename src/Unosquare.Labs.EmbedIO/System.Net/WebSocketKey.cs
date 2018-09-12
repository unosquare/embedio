namespace Unosquare.Net
{
    using System;
    using System.Text;
    using System.Security.Cryptography;

    internal class WebSocketKey
    {
        internal static readonly RandomNumberGenerator RandomNumber = RandomNumberGenerator.Create();
        private const string Guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        public WebSocketKey(bool init)
        {
            if (!init) return;

            var src = new byte[16];
            RandomNumber.GetBytes(src);

            KeyValue = Convert.ToBase64String(src);
        }

        public string KeyValue { get; set; }

        internal string CreateResponseKey()
        {
            var buff = new StringBuilder(KeyValue, 64);
            buff.Append(Guid);
            var sha1 = SHA1.Create();
            var src = sha1.ComputeHash(Encoding.UTF8.GetBytes(buff.ToString()));

            return Convert.ToBase64String(src);
        }
    }
}
