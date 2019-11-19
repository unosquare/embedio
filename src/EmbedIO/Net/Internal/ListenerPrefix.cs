using System;
using System.Globalization;

namespace EmbedIO.Net.Internal
{
    internal sealed class ListenerPrefix
    {
        public ListenerPrefix(string uri)
        {
            var defaultPort = 80;

            if (uri.StartsWith("https://"))
            {
                defaultPort = 443;
                Secure = true;
            }

            var length = uri.Length;
            var startHost = uri.IndexOf(':') + 3;

            if (startHost >= length)
                throw new ArgumentException("No host specified.");

            var colon = uri.LastIndexOf(':');
            int root;

            if (colon > 0)
            {
                Host = uri.Substring(startHost, colon - startHost);
                root = uri.IndexOf('/', colon, length - colon);
                Port = int.Parse(uri.Substring(colon + 1, root - colon - 1), CultureInfo.InvariantCulture);
            }
            else
            {
                root = uri.IndexOf('/', startHost, length - startHost);
                Host = uri.Substring(startHost, root - startHost);
                Port = defaultPort;
            }

            Path = uri.Substring(root);

            if (Path.Length != 1)
                Path = Path.Substring(0, Path.Length - 1);
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

            if (!uri.StartsWith("http://") && !uri.StartsWith("https://"))
                throw new ArgumentException("Only 'http' and 'https' schemes are supported.");

            var length = uri.Length;
            var startHost = uri.IndexOf(':') + 3;

            if (startHost >= length)
                throw new ArgumentException("No host specified.");

            var colon = uri.Substring(startHost).IndexOf(':') > 0 ? uri.LastIndexOf(':') : -1;

            if (startHost == colon)
                throw new ArgumentException("No host specified.");

            int root;
            if (colon > 0)
            {
                root = uri.IndexOf('/', colon, length - colon);
                if (root == -1)
                    throw new ArgumentException("No path specified.");

                if (!int.TryParse(uri.Substring(colon + 1, root - colon - 1), out var p) || p <= 0 || p >= 65536)
                    throw new ArgumentException("Invalid port.");
            }
            else
            {
                root = uri.IndexOf('/', startHost, length - startHost);
                if (root == -1)
                    throw new ArgumentException("No path specified.");
            }

            if (uri[uri.Length - 1] != '/')
                throw new ArgumentException("The prefix must end with '/'");
        }

        public bool IsValid() => Path.IndexOf('%') == -1 && Path.IndexOf("//", StringComparison.Ordinal) == -1;

        public override string ToString() => $"{Host}:{Port} ({(Secure ? "Secure" : "Insecure")}";
    }
}