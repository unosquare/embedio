using System;
using System.Globalization;

namespace EmbedIO.Net.Internal
{
    internal sealed class ListenerPrefix
    {
        public ListenerPrefix(string uri)
        {
            var parsedUri = new Uri(uri);
            Secure = parsedUri.Scheme == "https";
            Host = parsedUri.Host;
            Port = parsedUri.Port;
            Path = parsedUri.AbsolutePath;
        }

        public HttpListener? Listener { get; set; }

        public bool Secure { get; }

        public string Host { get; }

        public int Port { get; }

        public string Path { get; }

        public static void CheckUri(string uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            var parsedUri = new Uri(uri);

            if (parsedUri.Scheme != "http" && parsedUri.Scheme != "https")
                throw new ArgumentException("Only 'http' and 'https' schemes are supported.");

            if (parsedUri.Port <= 0 || parsedUri.Port >= 65536)
                throw new ArgumentException("Invalid port.");
        }

        public bool IsValid() => Path.IndexOf('%') == -1 && Path.IndexOf("//", StringComparison.Ordinal) == -1;

        public override string ToString() => $"{Host}:{Port} ({(Secure ? "Secure" : "Insecure")}";
    }
}