using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace EmbedIO
{
    partial class WebServer
    {
        private static readonly Lazy<(bool IPv4, bool IPv6)> IPSupport = new Lazy<(bool IPv4, bool IPv6)>(DetectIPSupport, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Gets whether IPv4 is supported on the machine.
        /// </summary>
        public static bool IsIPv4Supported => IPSupport.Value.IPv4;

        /// <summary>
        /// Gets whether IPv6 is supported on the machine.
        /// </summary>
        public static bool IsIPv6Supported => IPSupport.Value.IPv6;

        private static (bool IPv4, bool IPv6) DetectIPSupport()
        {
            string hostName;
            try
            {
                hostName = Dns.GetHostName();
            }
            catch (SocketException)
            {
                return (false, false);
            }

            IPHostEntry hostEntry;
            try
            {
                hostEntry = Dns.GetHostEntry(hostName);
            }
            catch (SocketException)
            {
                return (false, false);
            }

            var v4 = false;
            var v6 = false;
            foreach (var address in hostEntry.AddressList)
            {
                switch (address.AddressFamily)
                {
                    case AddressFamily.InterNetwork:
                        v4 = true;
                        break;
                    case AddressFamily.InterNetworkV6:
                        v6 = true;
                        break;
                }

                if (v4 && v6)
                    break;
            }

            return (v4, v6);
        }
    }
}