using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using EmbedIO.Net.Internal;
using Swan.Logging;

namespace EmbedIO.Net
{
    /// <summary>
    /// Represents the EndPoint Manager.
    /// </summary>
    public static class EndPointManager
    {
        private static readonly ConcurrentDictionary<IPAddress, ConcurrentDictionary<int, EndPointListener>> IPToEndpoints =
            new ConcurrentDictionary<IPAddress, ConcurrentDictionary<int, EndPointListener>>();

        /// <summary>
        /// Gets or sets a value indicating whether [use IPv6]. By default, this flag is set.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [use IPv6]; otherwise, <c>false</c>.
        /// </value>
        public static bool UseIpv6 { get; set; } = true;

        internal static void AddListener(HttpListener listener)
        {
            var added = new List<string>();

            try
            {
                foreach (var prefix in listener.Prefixes)
                {
                    AddPrefix(prefix, listener);
                    added.Add(prefix);
                }
            }
            catch (Exception ex)
            {
                ex.Log(nameof(AddListener));

                foreach (var prefix in added)
                {
                    RemovePrefix(prefix, listener);
                }

                throw;
            }
        }

        internal static void RemoveEndPoint(EndPointListener epl, IPEndPoint ep)
        {
            if (IPToEndpoints.TryGetValue(ep.Address, out var p))
            {
                if (p.TryRemove(ep.Port, out _) && p.Count == 0)
                {
                    IPToEndpoints.TryRemove(ep.Address, out _);
                }
            }

            epl.Dispose();
        }

        internal static void RemoveListener(HttpListener listener)
        {
            foreach (var prefix in listener.Prefixes)
            {
                RemovePrefix(prefix, listener);
            }
        }

        internal static void AddPrefix(string p, HttpListener listener)
        {
            var lp = new ListenerPrefix(p);

            if (!lp.IsValid())
                throw new HttpListenerException(400, "Invalid path.");

            // listens on all the interfaces if host name cannot be parsed by IPAddress.
            var epl = GetEpListener(lp.Host, lp.Port, listener, lp.Secure);
            epl.AddPrefix(lp, listener);
        }

        private static EndPointListener GetEpListener(string host, int port, HttpListener listener, bool secure = false)
        {
            var address = ResolveAddress(host);

            var p = IPToEndpoints.GetOrAdd(address, x => new ConcurrentDictionary<int, EndPointListener>());
            return p.GetOrAdd(port, x => new EndPointListener(listener, address, x, secure));
        }

        private static IPAddress ResolveAddress(string host)
        {
            if (host == "*" || host == "+" || host == "0.0.0.0")
                return UseIpv6 ? IPAddress.IPv6Any : IPAddress.Any;

            if (IPAddress.TryParse(host, out var address)) 
                return address;

            try
            {
                var hostEntry = new IPHostEntry {
                    HostName = host,
                    AddressList = Dns.GetHostAddresses(host),
                };

                return hostEntry.AddressList[0];
            }
            catch
            {
                return UseIpv6 ? IPAddress.IPv6Any : IPAddress.Any;
            }
        }

        private static void RemovePrefix(string prefix, HttpListener listener)
        {
            try
            {
                var lp = new ListenerPrefix(prefix);

                if (!lp.IsValid())
                    return;

                var epl = GetEpListener(lp.Host, lp.Port, listener, lp.Secure);
                epl.RemovePrefix(lp);
            }
            catch (SocketException)
            {
                // ignored
            }
        }
    }
}