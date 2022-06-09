using System;

namespace EmbedIO.Net.Internal
{
    internal class ListenerUri
    {
        private ListenerUri(bool secure,
                            string host,
                            int port,
                            string path)
        {
            Secure = secure;
            Host = host;
            Port = port;
            Path = path;
        }

        public bool Secure { get; private set; }

        public string Host { get; private set; }

        public int Port { get; private set; }

        public string Path { get; private set; }

        public static ListenerUri Parse(string uri)
        {
            bool secure;
            int port;
            int parsingPosition;
            if (uri.StartsWith("http://"))
            {
                secure = false;
                port = 80;
                parsingPosition = "http://".Length;
            } 
            else if (uri.StartsWith("https://"))
            {
                secure = true;
                port = 443;
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
                if (!int.TryParse(uri.Substring(startOfPortWithColon + 1, startOfPath - startOfPortWithColon - 1), out port) || port <= 0 || port >= 65535)
                {
                    throw new ArgumentException("Invalid port.");
                }
            }

            var host = uri.Substring(parsingPosition, (startOfPortWithColon == -1 ? startOfPath : startOfPortWithColon) - parsingPosition);
            var path = uri.Substring(startOfPath);
            if (!path.EndsWith("/"))
            {
                throw new ArgumentException("Path should end in '/'.");
            }

            return new ListenerUri(secure, host, port, path);
        }
    }
}