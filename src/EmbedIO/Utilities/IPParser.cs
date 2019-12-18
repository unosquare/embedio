using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace EmbedIO.Utilities
{
    public static class IPParser
    {
        /// <summary>
        /// Parses the specified IP address.
        /// </summary>
        /// <param name="address">The IP address.</param>
        /// <returns>A collection of <see cref="IPAddress"/> parsed correctly from <paramref name="address"/>.</returns>
        public static IEnumerable<IPAddress> Parse(string address)
        {
            var ipList = new List<IPAddress>();

            if (string.IsNullOrWhiteSpace(address))
                return ipList;

            if (IPAddress.TryParse(address, out var _ip))
            {
                ipList.Add(_ip);
                return ipList;
            }

            try
            {
                var entries = Dns.GetHostEntry(address);
                return entries.AddressList;
            }
            catch
            {
                // Ignore
            }

            if (IsCIDRNotation(address))
                return ParseCIDRNotation(address);

            if (IsSimpleIPRange(address))
                return TryParseSimpleIPRange(address);

            return ipList;
        }

        /// <summary>
        /// Determines whether the IP-range string is in CIDR notation.
        /// </summary>
        /// <param name="range">The IP-range string.</param>
        /// <returns>
        ///   <c>true</c> if the IP-range string is CIDR notation; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsCIDRNotation(string range)
        {
            var parts = range.Split('/');
            if (parts.Length != 2)
                return false;

            var prefix = parts[0];
            var prefixLen = parts[1];

            var prefixParts = prefix.Split('.');
            if (prefixParts.Length != 4)
                return false;

            return byte.TryParse(prefixLen, out var _len) && _len >= 0 && _len <= 32;
        }

        /// <summary>
        /// Parse IP-range string in CIDR notation. For example "12.15.0.0/16".
        /// </summary>
        /// <param name="range">The IP-range string.</param>
        /// <returns>A collection of <see cref="IPAddress"/> parsed correctly from <paramref name="range"/>.</returns>
        public static IEnumerable<IPAddress> ParseCIDRNotation(string range)
        {
            var ipList = new List<IPAddress>();

            if (!IsCIDRNotation(range))
                return ipList;

            var parts = range.Split('/');
            var prefix = parts[0];
            var prefixLen = byte.Parse(parts[1]);
            var prefixParts = prefix.Split('.');
            
            uint ip = 0;
            for (int i = 0; i < 4; i++)
            {
                ip = ip << 8;
                ip += uint.Parse(prefixParts[i]);
            }

            var shiftBits = (byte)(32 - prefixLen);
            uint ip1 = (ip >> shiftBits) << shiftBits;

            if ((ip1 & ip) != ip1) // Check correct subnet address
                return ipList;

            uint ip2 = ip1 >> shiftBits;
            for (int k = 0; k < shiftBits; k++)
            {
                ip2 = (ip2 << 1) + 1;
            }

            var beginIP = new byte[4];
            var endIP = new byte[4];

            for (int i = 0; i < 4; i++)
            {
                beginIP[i] = (byte)((ip1 >> (3 - i) * 8) & 255);
                endIP[i] = (byte)((ip2 >> (3 - i) * 8) & 255);
            }

            return GetAllIP(beginIP, endIP);
        }

        /// <summary>
        /// Determines whether the IP-range string is in simple IP range notation.
        /// </summary>
        /// <param name="range">The IP-range string.</param>
        /// <returns>
        ///   <c>true</c> if the IP-range string is in simple IP range notation; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsSimpleIPRange(string range)
        {
            var parts = range.Split('.');
            if (parts.Length != 4)
                return false;

            foreach (var part in parts)
            {
                var rangeParts = part.Split('-');
                if (rangeParts.Length < 1 || rangeParts.Length > 2)
                    return false;

                if (!byte.TryParse(rangeParts[0], out var _) ||
                    (rangeParts.Length > 1 && !byte.TryParse(rangeParts[1], out var _)))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Tries Parse IP-range string "12.15-16.1-30.10-255"
        /// </summary>
        /// <param name="range">The IP-range string.</param>
        /// <returns>A collection of <see cref="IPAddress"/> parsed correctly from <paramref name="range"/>.</returns>
        public static IEnumerable<IPAddress> TryParseSimpleIPRange(string range)
        {
            var ipList = new List<IPAddress>();

            if (!IsSimpleIPRange(range))
                return ipList;

            var beginIP = new byte[4];
            var endIP = new byte[4];

            var parts = range.Split('.');
            for (int i = 0; i < 4; i++)
            {
                var rangeParts = parts[i].Split('-');
                beginIP[i] = byte.Parse(rangeParts[0]);
                endIP[i] = (rangeParts.Length == 1) ? beginIP[i] : byte.Parse(rangeParts[1]);
            }

            return GetAllIP(beginIP, endIP);
        }

        private static IEnumerable<IPAddress> GetAllIP(byte[] beginIP, byte[] endIP)
        {
            int capacity = 1;
            for (int i = 0; i < 4; i++)
                capacity *= endIP[i] - beginIP[i] + 1;

            List<IPAddress> ips = new List<IPAddress>(capacity);
            for (int i0 = beginIP[0]; i0 <= endIP[0]; i0++)
            {
                for (int i1 = beginIP[1]; i1 <= endIP[1]; i1++)
                {
                    for (int i2 = beginIP[2]; i2 <= endIP[2]; i2++)
                    {
                        for (int i3 = beginIP[3]; i3 <= endIP[3]; i3++)
                        {
                            ips.Add(new IPAddress(new byte[] { (byte)i0, (byte)i1, (byte)i2, (byte)i3 }));
                        }
                    }
                }
            }

            return ips;
        }
    }
}
