using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Swan.Logging;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// Provides standard methods to parse IP address strings.
    /// </summary>
    public static class IPParser
    {
        /// <summary>
        /// Parses the specified IP address.
        /// </summary>
        /// <param name="address">The IP address.</param>
        /// <returns>A collection of <see cref="IPAddress"/> parsed correctly from <paramref name="address"/>.</returns>
        public static async Task<IEnumerable<IPAddress>> ParseAsync(string address)
        {
            if (address == null)
                return Enumerable.Empty<IPAddress>();

            if (IPAddress.TryParse(address, out var ip))
                return new List<IPAddress> { ip };

            try
            {
                return await Dns.GetHostAddressesAsync(address).ConfigureAwait(false);
            }
            catch (SocketException socketEx)
            {
                socketEx.Log(nameof(IPParser));
            }
            catch
            {
                // Ignore
            }

            if (IsCidrNotation(address))
                return ParseCidrNotation(address);

            return IsSimpleIPRange(address) ? TryParseSimpleIPRange(address) : Enumerable.Empty<IPAddress>();
        }

        /// <summary>
        /// Determines whether the IP-range string is in CIDR notation.
        /// </summary>
        /// <param name="range">The IP-range string.</param>
        /// <returns>
        ///   <c>true</c> if the IP-range string is CIDR notation; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsCidrNotation(string range)
        {
            if (string.IsNullOrWhiteSpace(range))
                return false;

            var parts = range.Split('/');
            if (parts.Length != 2)
                return false;

            var prefix = parts[0];
            var prefixLen = parts[1];

            var prefixParts = prefix.Split('.');
            if (prefixParts.Length != 4)
                return false;

            return byte.TryParse(prefixLen, out var len) && len <= 32;
        }

        /// <summary>
        /// Parse IP-range string in CIDR notation. For example "12.15.0.0/16".
        /// </summary>
        /// <param name="range">The IP-range string.</param>
        /// <returns>A collection of <see cref="IPAddress"/> parsed correctly from <paramref name="range"/>.</returns>
        public static IEnumerable<IPAddress> ParseCidrNotation(string range)
        {
            if (!IsCidrNotation(range))
                return Enumerable.Empty<IPAddress>();

            var parts = range.Split('/');
            var prefix = parts[0];
            
            if (!byte.TryParse(parts[1], out var prefixLen))
                return Enumerable.Empty<IPAddress>();

            var prefixParts = prefix.Split('.');
            if (prefixParts.Select(x => byte.TryParse(x, out _)).Any(x => !x))
                return Enumerable.Empty<IPAddress>();

            uint ip = 0;
            for (var i = 0; i < 4; i++)
            {
                ip <<= 8;
                ip += uint.Parse(prefixParts[i], NumberFormatInfo.InvariantInfo);
            }

            var shiftBits = (byte)(32 - prefixLen);
            var ip1 = (ip >> shiftBits) << shiftBits;

            if ((ip1 & ip) != ip1) // Check correct subnet address
                return Enumerable.Empty<IPAddress>();

            var ip2 = ip1 >> shiftBits;
            for (var k = 0; k < shiftBits; k++)
            {
                ip2 = (ip2 << 1) + 1;
            }

            var beginIP = new byte[4];
            var endIP = new byte[4];

            for (var i = 0; i < 4; i++)
            {
                beginIP[i] = (byte)((ip1 >> ((3 - i) * 8)) & 255);
                endIP[i] = (byte)((ip2 >> ((3 - i) * 8)) & 255);
            }

            return GetAllIPAddresses(beginIP, endIP);
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
            if (string.IsNullOrWhiteSpace(range))
                return false;

            var parts = range.Split('.');
            if (parts.Length != 4)
                return false;

            foreach (var part in parts)
            {
                var rangeParts = part.Split('-');
                if (rangeParts.Length < 1 || rangeParts.Length > 2)
                    return false;

                if (!byte.TryParse(rangeParts[0], out _) ||
                    (rangeParts.Length > 1 && !byte.TryParse(rangeParts[1], out _)))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Tries to parse IP-range string "12.15-16.1-30.10-255"
        /// </summary>
        /// <param name="range">The IP-range string.</param>
        /// <returns>A collection of <see cref="IPAddress"/> parsed correctly from <paramref name="range"/>.</returns>
        public static IEnumerable<IPAddress> TryParseSimpleIPRange(string range)
        {
            if (!IsSimpleIPRange(range))
                return Enumerable.Empty<IPAddress>();

            var beginIP = new byte[4];
            var endIP = new byte[4];

            var parts = range.Split('.');
            for (var i = 0; i < 4; i++)
            {
                var rangeParts = parts[i].Split('-');
                beginIP[i] = byte.Parse(rangeParts[0], NumberFormatInfo.InvariantInfo);
                endIP[i] = (rangeParts.Length == 1) ? beginIP[i] : byte.Parse(rangeParts[1], NumberFormatInfo.InvariantInfo);
            }

            return GetAllIPAddresses(beginIP, endIP);
        }

        private static IEnumerable<IPAddress> GetAllIPAddresses(byte[] beginIP, byte[] endIP)
        {
            for (var i = 0; i < 4; i++)
            {
                if (endIP[i] < beginIP[i])
                    return Enumerable.Empty<IPAddress>();
            }
            
            var capacity = 1;
            for (var i = 0; i < 4; i++)
                capacity *= endIP[i] - beginIP[i] + 1;

            var ips = new List<IPAddress>(capacity);
            for (int i0 = beginIP[0]; i0 <= endIP[0]; i0++)
            {
                for (int i1 = beginIP[1]; i1 <= endIP[1]; i1++)
                {
                    for (int i2 = beginIP[2]; i2 <= endIP[2]; i2++)
                    {
                        for (int i3 = beginIP[3]; i3 <= endIP[3]; i3++)
                        {
                            ips.Add(new IPAddress(new[] { (byte)i0, (byte)i1, (byte)i2, (byte)i3 }));
                        }
                    }
                }
            }

            return ips;
        }
    }
}
