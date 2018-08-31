﻿namespace Unosquare.Net
{
    using System;
    using System.Collections.Generic;
    using System.Net;

    internal static class EndPointManager
    {
        private static readonly Dictionary<IPAddress, Dictionary<int, EndPointListener>> IPToEndpoints =
            new Dictionary<IPAddress, Dictionary<int, EndPointListener>>();

        public static void AddListener(HttpListener listener)
        {
            var added = new List<string>();

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
                foreach (var prefix in added)
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

        public static void RemoveEndPoint(EndPointListener epl, IPEndPoint ep)
        {
            lock (IPToEndpoints)
            {
                var p = IPToEndpoints[ep.Address];
                p.Remove(ep.Port);
                if (p.Count == 0)
                {
                    IPToEndpoints.Remove(ep.Address);
                }
            }

            epl.Close();
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

        private static EndPointListener GetEpListener(string host, int port, HttpListener listener, bool secure = false)
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
                        AddressList = Dns.GetHostAddressesAsync(host).Result,
                    };

                    addr = iphost.AddressList[0];
                }
                catch
                {
                    addr = IPAddress.Any;
                }
            }

            Dictionary<int, EndPointListener> p;
            if (IPToEndpoints.ContainsKey(addr))
            {
                p = IPToEndpoints[addr];
            }
            else
            {
                p = new Dictionary<int, EndPointListener>();
                IPToEndpoints[addr] = p;
            }

            EndPointListener epl;
            if (p.ContainsKey(port))
            {
                epl = p[port];
            }
            else
            {
                epl = new EndPointListener(listener, addr, port, secure);
                p[port] = epl;
            }

            return epl;
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