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

    internal class ListenerUri
    {
        public bool Secure { get; private set; }

        public string Host { get; private set; }

        public int Port { get; private set; }

        public string Path { get; private set; }

        public static ListenerUri Parse(string uri)
        {
            var result = new ListenerUri();

            int parsingPosition;
            if (uri.StartsWith("http://"))
            {
                result.Secure = false;
                result.Port = 80;
                parsingPosition = "http://".Length;
            } 
            else if (uri.StartsWith("https://"))
            {
                result.Secure = true;
                result.Port = 443;
                parsingPosition = "https://".Length;
            }
            else
            {
                throw new Exception("Only 'http' and 'https' schemes are supported.");
            }

            var startOfPath = uri.IndexOf('/', parsingPosition);

            if (startOfPath == -1)
            {
                throw new ArgumentException("Path should end in '/'.");
            }

            var hostWithPort = uri.Substring(parsingPosition, startOfPath - parsingPosition);

            var startOfPortWithColon = hostWithPort.LastIndexOf(':');
            if (startOfPortWithColon > -1)
            {
                startOfPortWithColon += parsingPosition;
            }
            var endOfIpV6 = hostWithPort.LastIndexOf(']');
            if (endOfIpV6 > -1)
            {
                endOfIpV6 += parsingPosition;
            }
            if (endOfIpV6 > startOfPortWithColon)
            {
                startOfPortWithColon = -1;
            }
            if (startOfPortWithColon != -1 && startOfPortWithColon < startOfPath)
            {
                if (!int.TryParse(uri.Substring(startOfPortWithColon + 1, startOfPath - startOfPortWithColon - 1), out var port) || port <= 0 || port >= 65535)
                {
                    throw new ArgumentException("Invalid port.");
                }

                result.Port = port;
            }

            result.Host = uri.Substring(parsingPosition, (startOfPortWithColon == -1 ? startOfPath : startOfPortWithColon) - parsingPosition);
            result.Path = uri.Substring(startOfPath);
            if (!result.Path.EndsWith("/"))
            {
                throw new ArgumentException("Path should end in '/'.");
            }

            return result;
        }
    }
}