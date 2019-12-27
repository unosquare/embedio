using EmbedIO.Utilities;
using Swan;
using Swan.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace EmbedIO.Security
{
    /// <summary>
    /// A module to ban clients by IP address, based on TCP requests-per-second or RegEx matches on log messages.
    /// </summary>
    /// <seealso cref="WebModuleBase" />
    public class IPBanningModule : WebModuleBase
    {
        /// <summary>
        /// The default ban minutes.
        /// </summary>
        public const int DefaultBanMinutes = 30;
        private static readonly ConcurrentDictionary<IPAddress, BanInfo> Blacklist = new ConcurrentDictionary<IPAddress, BanInfo>();
        private static readonly ConcurrentBag<IPAddress> Whitelist = new ConcurrentBag<IPAddress>();
        private static readonly ConcurrentBag<IIPBanningCriterion> BanningCriterions = new ConcurrentBag<IIPBanningCriterion>();
        private static readonly PeriodicTask? Purger;

        private readonly int _banMinutes;

        private bool _disposed;
        
        static IPBanningModule()
        {
            Purger = new PeriodicTask(TimeSpan.FromMinutes(1), ct =>
                {
                    PurgeBlackList();
                    foreach (var criterion in BanningCriterions)
                    {
                        criterion.PurgeData();
                    }

                    return Task.CompletedTask;
                });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IPBanningModule" /> class.
        /// </summary>
        /// <param name="baseRoute">The base route.</param>
        /// <param name="whitelist">A collection of valid IPs that never will be banned.</param>
        /// <param name="banMinutes">Minutes that an IP will remain banned.</param>
        public IPBanningModule(string baseRoute,
            IEnumerable<string>? whitelist = null,
            int banMinutes = DefaultBanMinutes)
            : base(baseRoute)
        {
            _banMinutes = banMinutes;
            AddToWhitelist(whitelist);
        }

        /// <inheritdoc />
        public override bool IsFinalHandler => false;
        
        private IPAddress? ClientAddress { get; set; }

        /// <summary>
        /// Gets the list of current banned IPs.
        /// </summary>
        /// <returns>A collection of <see cref="BanInfo"/> in the blacklist.</returns>
        public static IEnumerable<BanInfo> GetBannedIPs() =>
            Blacklist.Values.ToList();

        /// <summary>
        /// Tries to ban an IP explicitly.
        /// </summary>
        /// <param name="address">The IP address to ban.</param>
        /// <param name="banMinutes">Minutes that the IP will remain banned.</param>
        /// <param name="isExplicit"><c>true</c> if the IP was explicitly banned.</param>
        /// <returns>
        ///     <c>true</c> if the IP was added to the blacklist; otherwise, <c>false</c>.
        /// </returns>
        public static bool TryBanIP(IPAddress address, int banMinutes, bool isExplicit = true) =>
            TryBanIP(address, DateTime.Now.AddMinutes(banMinutes), isExplicit);

        /// <summary>
        /// Tries to ban an IP explicitly.
        /// </summary>
        /// <param name="address">The IP address to ban.</param>
        /// <param name="banDuration">A <see cref="TimeSpan"/> specifying the duration that the IP will remain banned.</param>
        /// <param name="isExplicit"><c>true</c> if the IP was explicitly banned.</param>
        /// <returns>
        ///     <c>true</c> if the IP was added to the blacklist; otherwise, <c>false</c>.
        /// </returns>
        public static bool TryBanIP(IPAddress address, TimeSpan banDuration, bool isExplicit = true) =>
            TryBanIP(address, DateTime.Now.Add(banDuration), isExplicit);

        /// <summary>
        /// Tries to ban an IP explicitly.
        /// </summary>
        /// <param name="address">The IP address to ban.</param>
        /// <param name="banUntil">A <see cref="DateTime"/> specifying the expiration time of the ban.</param>
        /// <param name="isExplicit"><c>true</c> if the IP was explicitly banned.</param>
        /// <returns>
        ///     <c>true</c> if the IP was added to the blacklist; otherwise, <c>false</c>.
        /// </returns>
        public static bool TryBanIP(IPAddress address, DateTime banUntil, bool isExplicit = true)
        {
            try
            {
                Blacklist.AddOrUpdate(address,
                    k =>
                        new BanInfo()
                        {
                            IPAddress = k,
                            ExpiresAt = banUntil.Ticks,
                            IsExplicit = isExplicit,
                        },
                    (k, v) =>
                        new BanInfo()
                        {
                            IPAddress = k,
                            ExpiresAt = banUntil.Ticks,
                            IsExplicit = isExplicit,
                        }
                );
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to unban an IP explicitly.
        /// </summary>
        /// <param name="address">The IP address.</param>
        /// <returns>
        ///     <c>true</c> if the IP was removed from the blacklist; otherwise, <c>false</c>.
        /// </returns>
        public static bool TryUnbanIP(IPAddress address) =>
            Blacklist.TryRemove(address, out _);
        
        /// <inheritdoc />
        public void Dispose() =>
            Dispose(true);

        internal void AddToWhitelist(IEnumerable<string> whitelist) =>
            AddToWhitelistAsync(whitelist).GetAwaiter().GetResult();

        internal async Task AddToWhitelistAsync(IEnumerable<string> whitelist)
        {
            if (whitelist?.Any() != true)
                return;

            foreach (var address in whitelist)
            {
                var addressees = await IPParser.ParseAsync(address).ConfigureAwait(false);
                foreach (var ipAddress in addressees.Where(x => !Whitelist.Contains(x)))
                {
                    Whitelist.Add(ipAddress);
                }
            }
        }
        
        /// <inheritdoc />
        protected override async Task OnRequestAsync(IHttpContext context)
        {
            ClientAddress = context.Request.RemoteEndPoint.Address;

            if (Whitelist.Contains(ClientAddress))
                return;

            foreach (var criterion in BanningCriterions)
            {
                await criterion.UpdateData(ClientAddress);
            }

            if (Blacklist.ContainsKey(ClientAddress))
                throw HttpException.Forbidden();
        }
        
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                
            }

            _disposed = true;
        }

        private static void PurgeBlackList()
        {
            foreach (var k in Blacklist.Keys)
            {
                if (Blacklist.TryGetValue(k, out var info) &&
                    DateTime.Now.Ticks > info.ExpiresAt)
                    Blacklist.TryRemove(k, out _);
            }
        }
    }
}
