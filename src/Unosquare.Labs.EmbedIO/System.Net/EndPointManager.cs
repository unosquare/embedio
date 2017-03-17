#if !NET46
//
// System.Net.EndPointManager
//
// Author:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
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
using System.Collections;
using System.Net;

namespace Unosquare.Net
{
    internal static class EndPointManager
    {
        private static readonly Hashtable IPToEndpoints = new Hashtable();

        public static void AddListener(HttpListener listener)
        {
            var added = new ArrayList();
            try
            {
                lock (IPToEndpoints)
                {
                    foreach (var prefix in listener.Prefixes)
                    {
                        AddPrefixInternal(prefix, listener);
                        added.Add(prefix);
                    }
                }
            }
            catch
            {
                foreach (string prefix in added)
                {
                    RemovePrefix(prefix, listener);
                }
                throw;
            }
        }

        public static void AddPrefix(string prefix, HttpListener listener)
        {
            lock (IPToEndpoints)
            {
                AddPrefixInternal(prefix, listener);
            }
        }

        private static void AddPrefixInternal(string p, HttpListener listener)
        {
            var lp = new ListenerPrefix(p);
            if (lp.Path.IndexOf('%') != -1)
                throw new HttpListenerException(400, "Invalid path.");

            if (lp.Path.IndexOf("//", StringComparison.Ordinal) != -1) // TODO: Code?
                throw new HttpListenerException(400, "Invalid path.");

            // listens on all the interfaces if host name cannot be parsed by IPAddress.
            var epl = GetEpListener(lp.Host, lp.Port, listener, lp.Secure);
            epl.AddPrefix(lp, listener);
        }

        private static EndPointListener GetEpListener(string host, int port, HttpListener listener, bool secure)
        {
            IPAddress addr;

            if (host == "*")
            {
                addr = IPAddress.Any;
            }
            else if (IPAddress.TryParse(host, out addr) == false)
            {
                try
                {
                    var iphost = new IPHostEntry
                    {
                        HostName = host,
                        AddressList = Dns.GetHostAddressesAsync(host).Result
                    };

                    addr = iphost.AddressList[0];
                }
                catch
                {
                    addr = IPAddress.Any;
                }
            }

            Hashtable p;  // Dictionary<int, EndPointListener>
            if (IPToEndpoints.ContainsKey(addr))
            {
                p = (Hashtable)IPToEndpoints[addr];
            }
            else
            {
                p = new Hashtable();
                IPToEndpoints[addr] = p;
            }

            EndPointListener epl;
            if (p.ContainsKey(port))
            {
                epl = (EndPointListener)p[port];
            }
            else
            {
                epl = new EndPointListener(listener, addr, port, secure);
                p[port] = epl;
            }

            return epl;
        }

        public static void RemoveEndPoint(EndPointListener epl, IPEndPoint ep)
        {
            lock (IPToEndpoints)
            {
                // Dictionary<int, EndPointListener> p
                var p = (Hashtable)IPToEndpoints[ep.Address];
                p.Remove(ep.Port);
                if (p.Count == 0)
                {
                    IPToEndpoints.Remove(ep.Address);
                }
            }
            
            epl.CloseAsync().Wait(); // TODO: Is this right?
        }

        public static void RemoveListener(HttpListener listener)
        {
            lock (IPToEndpoints)
            {
                foreach (var prefix in listener.Prefixes)
                {
                    RemovePrefixInternal(prefix, listener);
                }
            }
        }

        public static void RemovePrefix(string prefix, HttpListener listener)
        {
            lock (IPToEndpoints)
            {
                RemovePrefixInternal(prefix, listener);
            }
        }

        private static void RemovePrefixInternal(string prefix, HttpListener listener)
        {
            var lp = new ListenerPrefix(prefix);
            if (lp.Path.IndexOf('%') != -1 || lp.Path.IndexOf("//", StringComparison.Ordinal) != -1)
                return;

            var epl = GetEpListener(lp.Host, lp.Port, listener, lp.Secure);
            epl.RemovePrefix(lp, listener);
        }
    }
}
#endif