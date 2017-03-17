#if !NET46
//
// System.Net.EndPointListener
//
// Author:
//	Gonzalo Paniagua Javier (gonzalo.mono@gmail.com)
//
// Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
// Copyright (c) 2012 Xamarin, Inc. (http://xamarin.com)
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
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

#if SSL
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
#endif

namespace Unosquare.Net
{
    internal sealed class EndPointListener
    {
        private readonly IPEndPoint _endpoint;
        private readonly Socket _sock;
        private Hashtable _prefixes; // Dictionary <ListenerPrefix, HttpListener>
        private ArrayList _unhandled; // List<ListenerPrefix> unhandled; host = '*'
        private ArrayList _all; // List<ListenerPrefix> all;  host = '+       
#if SSL
        private bool _secure = false;
        private X509Certificate _cert = null;
#endif
        private readonly Dictionary<HttpConnection, HttpConnection> _unregistered;

        public EndPointListener(HttpListener listener, IPAddress addr, int port, bool secure)
        {
            Listener = listener;

            if (secure)
            {
#if SSL
                _secure = secure;
				_cert = listener.LoadCertificateAndKey (addr, port);
#else
                throw new Exception("SSL is not supported");
#endif
            }

            _endpoint = new IPEndPoint(addr, port);
            _sock = new Socket(addr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _sock.Bind(_endpoint);
            _sock.Listen(500);
            var args = new SocketAsyncEventArgs {UserToken = this};
            args.Completed += OnAccept;
            Socket dummy = null;
            Accept(_sock, args, ref dummy);
            _prefixes = new Hashtable();
            _unregistered = new Dictionary<HttpConnection, HttpConnection>();
        }

        internal HttpListener Listener { get; }

        private static void Accept(Socket socket, SocketAsyncEventArgs e, ref Socket accepted)
        {
            e.AcceptSocket = null;
            bool asyn;
            try
            {
                asyn = socket.AcceptAsync(e);
            }
            catch
            {
                if (accepted != null)
                {
                    try
                    {
                        accepted.Dispose();
                    }
                    catch
                    {
                        // ignored
                    }
                    accepted = null;
                }
                return;
            }
            if (!asyn)
            {
                ProcessAccept(e);
            }
        }

        private static void ProcessAccept(SocketAsyncEventArgs args)
        {
            Socket accepted = null;
            if (args.SocketError == SocketError.Success)
                accepted = args.AcceptSocket;

            var epl = (EndPointListener) args.UserToken;

            Accept(epl._sock, args, ref accepted);
            if (accepted == null)
                return;

#if SSL
            if (epl._secure && epl._cert == null)
            {
                accepted.Dispose();
                return;
            }
            var conn = new HttpConnection(accepted, epl, epl._secure, epl._cert);
#else
            var conn = new HttpConnection(accepted, epl);
#endif
            lock (epl._unregistered)
            {
                epl._unregistered[conn] = conn;
            }

            conn.BeginReadRequest().Wait();
        }

        private static void OnAccept(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        internal void RemoveConnection(HttpConnection conn)
        {
            lock (_unregistered)
            {
                _unregistered.Remove(conn);
            }
        }

        public bool BindContext(HttpListenerContext context)
        {
            var req = context.Request;
            ListenerPrefix prefix;
            var listener = SearchListener(req.Url, out prefix);
            if (listener == null)
                return false;

            context.Listener = listener;
            context.Connection.Prefix = prefix;
            return true;
        }

        public void UnbindContext(HttpListenerContext context)
        {
            if (context?.Request == null)
                return;

            context.Listener.UnregisterContext(context);
        }

        private HttpListener SearchListener(Uri uri, out ListenerPrefix prefix)
        {
            prefix = null;
            if (uri == null)
                return null;

            var host = uri.Host;
            var port = uri.Port;
            var path = WebUtility.UrlDecode(uri.AbsolutePath);
            var pathSlash = path[path.Length - 1] == '/' ? path : path + "/";

            HttpListener bestMatch = null;
            var bestLength = -1;

            if (!string.IsNullOrEmpty(host))
            {
                var pRo = _prefixes;
                foreach (ListenerPrefix p in pRo.Keys)
                {
                    var ppath = p.Path;
                    if (ppath.Length < bestLength)
                        continue;

                    if (p.Host != host || p.Port != port)
                        continue;

                    if (path.StartsWith(ppath) || pathSlash.StartsWith(ppath))
                    {
                        bestLength = ppath.Length;
                        bestMatch = (HttpListener) pRo[p];
                        prefix = p;
                    }
                }
                if (bestLength != -1)
                    return bestMatch;
            }

            var list = _unhandled;
            bestMatch = MatchFromList(path, list, out prefix);
            if (path != pathSlash && bestMatch == null)
                bestMatch = MatchFromList(pathSlash, list, out prefix);
            if (bestMatch != null)
                return bestMatch;

            list = _all;
            bestMatch = MatchFromList(path, list, out prefix);
            if (path != pathSlash && bestMatch == null)
                bestMatch = MatchFromList(pathSlash, list, out prefix);

            return bestMatch;
        }

        private static HttpListener MatchFromList(string path, ArrayList list, out ListenerPrefix prefix)
        {
            prefix = null;
            if (list == null)
                return null;

            HttpListener bestMatch = null;
            var bestLength = -1;

            foreach (ListenerPrefix p in list)
            {
                var ppath = p.Path;
                if (ppath.Length < bestLength)
                    continue;

                if (path.StartsWith(ppath))
                {
                    bestLength = ppath.Length;
                    bestMatch = p.Listener;
                    prefix = p;
                }
            }

            return bestMatch;
        }

        private static void AddSpecial(ArrayList coll, ListenerPrefix prefix)
        {
            if (coll == null)
                return;

            foreach (ListenerPrefix p in coll)
            {
                if (p.Path == prefix.Path) //TODO: code
                    throw new HttpListenerException(400, "Prefix already in use.");
            }

            coll.Add(prefix);
        }

        private static bool RemoveSpecial(ArrayList coll, ListenerPrefix prefix)
        {
            if (coll == null)
                return false;

            var c = coll.Count;
            for (var i = 0; i < c; i++)
            {
                var p = (ListenerPrefix) coll[i];
                if (p.Path == prefix.Path)
                {
                    coll.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        private void CheckIfRemove()
        {
            if (_prefixes.Count > 0)
                return;

            var list = _unhandled;
            if (list != null && list.Count > 0)
                return;

            list = _all;
            if (list != null && list.Count > 0)
                return;

            EndPointManager.RemoveEndPoint(this, _endpoint);
        }

        public async Task CloseAsync()
        {
            _sock.Dispose();
            List<HttpConnection> connections;

            lock (_unregistered)
            {
                // Clone the list because RemoveConnection can be called from Close
                connections = new List<HttpConnection>(_unregistered.Keys);
                _unregistered.Clear();
            }

            foreach (var c in connections)
                await c.CloseAsync(true);
        }

        public void AddPrefix(ListenerPrefix prefix, HttpListener listener)
        {
            ArrayList current;
            ArrayList future;

            if (prefix.Host == "*")
            {
                do
                {
                    current = _unhandled;
                    future = (current != null) ? (ArrayList) current.Clone() : new ArrayList();
                    prefix.Listener = listener;
                    AddSpecial(future, prefix);
                } while (Interlocked.CompareExchange(ref _unhandled, future, current) != current);
                return;
            }

            if (prefix.Host == "+")
            {
                do
                {
                    current = _all;
                    future = (current != null) ? (ArrayList) current.Clone() : new ArrayList();
                    prefix.Listener = listener;
                    AddSpecial(future, prefix);
                } while (Interlocked.CompareExchange(ref _all, future, current) != current);
                return;
            }

            Hashtable prefs, p2;
            do
            {
                prefs = _prefixes;
                if (prefs.ContainsKey(prefix))
                {
                    var other = (HttpListener) prefs[prefix];
                    if (other != listener) // TODO: code.
                        throw new HttpListenerException(400, "There's another listener for " + prefix);
                    return;
                }
                p2 = (Hashtable) prefs.Clone();
                p2[prefix] = listener;
            } while (Interlocked.CompareExchange(ref _prefixes, p2, prefs) != prefs);
        }

        public void RemovePrefix(ListenerPrefix prefix, HttpListener listener)
        {
            ArrayList current;
            ArrayList future;
            if (prefix.Host == "*")
            {
                do
                {
                    current = _unhandled;
                    future = (current != null) ? (ArrayList) current.Clone() : new ArrayList();
                    if (!RemoveSpecial(future, prefix))
                        break; // Prefix not found
                } while (Interlocked.CompareExchange(ref _unhandled, future, current) != current);
                CheckIfRemove();
                return;
            }

            if (prefix.Host == "+")
            {
                do
                {
                    current = _all;
                    future = (current != null) ? (ArrayList) current.Clone() : new ArrayList();
                    if (!RemoveSpecial(future, prefix))
                        break; // Prefix not found
                } while (Interlocked.CompareExchange(ref _all, future, current) != current);
                CheckIfRemove();
                return;
            }

            Hashtable prefs, p2;
            do
            {
                prefs = _prefixes;
                if (!prefs.ContainsKey(prefix))
                    break;

                p2 = (Hashtable) prefs.Clone();
                p2.Remove(prefix);
            } while (Interlocked.CompareExchange(ref _prefixes, p2, prefs) != prefs);
            CheckIfRemove();
        }
    }
}

#endif