namespace Unosquare.Net
{
    using System;

    internal sealed class ListenerPrefix
    {
        private readonly string _original;

        public ListenerPrefix(string prefix)
        {
            _original = prefix;
            Parse(prefix);
        }

        public HttpListener Listener { get; set; }

        public bool Secure { get; private set; }

        public string Host { get; private set; }

        public int Port { get; private set; }

        public string Path { get; private set; }

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

            var colon = uri.LastIndexOf(':');
            if (startHost == colon)
                throw new ArgumentException("No host specified.");

            int root;
            if (colon > 0)
            {
                root = uri.IndexOf('/', colon, length - colon);
                if (root == -1)
                    throw new ArgumentException("No path specified.");

                try
                {
                    var p = int.Parse(uri.Substring(colon + 1, root - colon - 1));
                    if (p <= 0 || p >= 65536)
                        throw new InvalidOperationException();
                }
                catch
                {
                    throw new ArgumentException("Invalid port.");
                }
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

        private void Parse(string uri)
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
                Port = int.Parse(uri.Substring(colon + 1, root - colon - 1));
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
    }
}