#if !NET452
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

using System.Collections;

namespace System.Net
{
    static class EndPointManager
    {
        // Dictionary<IPAddress, Dictionary<int, EndPointListener>>
        static readonly Hashtable _ipToEndpoints = new Hashtable();

        public static void AddListener(HttpListener listener)
        {
            var added = new ArrayList();
            try
            {
                lock (_ipToEndpoints)
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
            lock (_ipToEndpoints)
            {
                AddPrefixInternal(prefix, listener);
            }
        }

        static void AddPrefixInternal(string p, HttpListener listener)
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

        static EndPointListener GetEpListener(string host, int port, HttpListener listener, bool secure)
        {
            IPAddress addr;
            if (host == "*")
                addr = IPAddress.Any;
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
            Hashtable p = null;  // Dictionary<int, EndPointListener>
            if (_ipToEndpoints.ContainsKey(addr))
            {
                p = (Hashtable)_ipToEndpoints[addr];
            }
            else
            {
                p = new Hashtable();
                _ipToEndpoints[addr] = p;
            }

            EndPointListener epl = null;
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
            lock (_ipToEndpoints)
            {
                // Dictionary<int, EndPointListener> p
                Hashtable p = null;
                p = (Hashtable)_ipToEndpoints[ep.Address];
                p.Remove(ep.Port);
                if (p.Count == 0)
                {
                    _ipToEndpoints.Remove(ep.Address);
                }
                epl.Close();
            }
        }

        public static void RemoveListener(HttpListener listener)
        {
            lock (_ipToEndpoints)
            {
                foreach (var prefix in listener.Prefixes)
                {
                    RemovePrefixInternal(prefix, listener);
                }
            }
        }

        public static void RemovePrefix(string prefix, HttpListener listener)
        {
            lock (_ipToEndpoints)
            {
                RemovePrefixInternal(prefix, listener);
            }
        }

        static void RemovePrefixInternal(string prefix, HttpListener listener)
        {
            var lp = new ListenerPrefix(prefix);
            if (lp.Path.IndexOf('%') != -1)
                return;

            if (lp.Path.IndexOf("//", StringComparison.Ordinal) != -1)
                return;

            var epl = GetEpListener(lp.Host, lp.Port, listener, lp.Secure);
            epl.RemovePrefix(lp, listener);
        }
    }
}
#endif