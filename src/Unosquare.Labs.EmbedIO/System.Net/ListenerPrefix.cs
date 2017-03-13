#if !NET46
//
// System.Net.ListenerPrefix
//
// Author:
//	Gonzalo Paniagua Javier (gonzalo@novell.com)
//	Oleg Mihailik (mihailik gmail co_m)
//
// Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Net;

namespace Unosquare.Net
{
    internal sealed class ListenerPrefix
    {
        readonly string _original;
        ushort _port;
        public HttpListener Listener;

        public ListenerPrefix(string prefix)
        {
            _original = prefix;
            Parse(prefix);
        }

        public override string ToString()
        {
            return _original;
        }

        public IPAddress[] Addresses { get; set; }

        public bool Secure { get; private set; }

        public string Host { get; private set; }

        public int Port => (int)_port;

        public string Path { get; private set; }

        // Equals and GetHashCode are required to detect duplicates in HttpListenerPrefixCollection.
        public override bool Equals(object o)
        {
            var other = o as ListenerPrefix;
            if (other == null)
                return false;

            return (_original == other._original);
        }

        public override int GetHashCode()
        {
            return _original.GetHashCode();
        }

        private void Parse(string uri)
        {
            ushort defaultPort = 80;
            if (uri.StartsWith("https://"))
            {
                defaultPort = 443;
                Secure = true;
            }

            var length = uri.Length;
            var startHost = uri.IndexOf(':') + 3;
            if (startHost >= length)
                throw new ArgumentException("No host specified.");

            var colon = uri.IndexOf(':', startHost, length - startHost);
            int root;
            if (colon > 0)
            {
                Host = uri.Substring(startHost, colon - startHost);
                root = uri.IndexOf('/', colon, length - colon);
                _port = (ushort)int.Parse(uri.Substring(colon + 1, root - colon - 1));
                Path = uri.Substring(root);
            }
            else
            {
                root = uri.IndexOf('/', startHost, length - startHost);
                Host = uri.Substring(startHost, root - startHost);
                _port = defaultPort;
                Path = uri.Substring(root);
            }
            if (Path.Length != 1)
                Path = Path.Substring(0, Path.Length - 1);
        }

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

            var colon = uri.IndexOf(':', startHost, length - startHost);
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
                        throw new Exception();
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
    }
}
#endif