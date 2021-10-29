using System;
using System.Globalization;

namespace EmbedIO.Net.Internal
{
    internal sealed class ListenerPrefix
    {
        public ListenerPrefix(string uri)
        {
            var parsedUri = ListenerUri.Parse(uri);
            Secure = parsedUri.Secure;
            Host = parsedUri.Host;
            Port = parsedUri.Port;
            Path = parsedUri.Path;
        }

        public HttpListener? Listener { get; set; }

        public bool Secure { get; }

        public string Host { get; }

        public int Port { get; }

        public string Path { get; }

        public static void CheckUri(string uri)
        {
            ListenerUri.Parse(uri);
        }

        public bool IsValid() => Path.IndexOf('%') == -1 && Path.IndexOf("//", StringComparison.Ordinal) == -1;

        public override string ToString() => $"{Host}:{Port} ({(Secure ? "Secure" : "Insecure")}";
    }
}