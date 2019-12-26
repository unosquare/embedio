using EmbedIO.Utilities;
using Swan;
using Swan.Logging;
using Swan.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
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

        /// <summary>
        /// The default maximum request per second.
        /// </summary>
        public const int DefaultMaxRequestsPerSecond = 50;

        /// <summary>
        /// The default matching period.
        /// </summary>
        public const int DefaultSecondsMatchingPeriod = 60;

        /// <summary>
        /// The default maximum match count per period.
        /// </summary>
        public const int DefaultMaxMatchCount = 10;

        private static readonly ConcurrentDictionary<IPAddress, ConcurrentBag<long>> Requests = new ConcurrentDictionary<IPAddress, ConcurrentBag<long>>();
        private static readonly ConcurrentDictionary<IPAddress, ConcurrentBag<long>> FailRegexMatches = new ConcurrentDictionary<IPAddress, ConcurrentBag<long>>();
        private static readonly ConcurrentDictionary<IPAddress, BanInfo> Blacklist = new ConcurrentDictionary<IPAddress, BanInfo>();
        private static readonly ConcurrentDictionary<string, Regex> FailRegex = new ConcurrentDictionary<string, Regex>();
        private static readonly PeriodicTask? Purger;
        private static int SecondsMatchingPeriod = DefaultSecondsMatchingPeriod;

        private readonly int _banMinutes;
        private readonly int _maxRequestsPerSecond=50;
        private readonly int _maxMatchCount;
        private bool _disposed;
        private ILogger _innerLogger;

        static IPBanningModule()
        {
            Purger = new PeriodicTask(TimeSpan.FromMinutes(1), ct =>
                {
                    PurgeBlackList();
                    PurgeRequests();
                    PurgeFailRegexMatches();

                    return Task.CompletedTask;
                });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IPBanningModule"/> class.
        /// </summary>
        /// <param name="baseRoute">The base route.</param>
        /// <param name="failRegex">A collection of regex to match log messages against.</param>
        /// <param name="banMinutes">Minutes that an IP will remain banned.</param>
        /// <param name="maxRetry">The maximum number of failed attempts before banning an IP.</param>
        public IPBanningModule(string baseRoute,
            IEnumerable<string> failRegex,
            int banMinutes = DefaultBanMinutes,
            int maxRetry = DefaultMaxMatchCount)
            : this(baseRoute, failRegex, null, banMinutes, maxRetry)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IPBanningModule"/> class.
        /// </summary>
        /// <param name="baseRoute">The base route.</param>
        /// <param name="failRegex">A collection of regex to match log messages against.</param>
        /// <param name="whitelist">A collection of valid IPs that never will be banned.</param>
        /// <param name="banMinutes">Minutes that an IP will remain banned.</param>
        /// <param name="maxRetry">The maximum number of failed attempts before banning an IP.</param>
        public IPBanningModule(string baseRoute,
            IEnumerable<string>? failRegex = null,
            IEnumerable<string>? whitelist = null,
            int banMinutes = DefaultBanMinutes,
            int maxRetry = DefaultMaxMatchCount)
            : base(baseRoute)
        {
            if (failRegex != null)
                AddRules(failRegex);

            _banMinutes = banMinutes;
            _maxMatchCount = maxRetry;
            AddToWhitelist(whitelist);
            _innerLogger = new InnerIPBanningModuleLogger(this);
        }

        /// <inheritdoc />
        public override bool IsFinalHandler => false;
        
        private IPAddress? ClientAddress { get; set; }

        private ConcurrentBag<IPAddress> Whitelist { get; } = new ConcurrentBag<IPAddress>();

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

        internal void AddRules(IEnumerable<string> patterns)
        {
            foreach (var pattern in patterns)
                AddRule(pattern);
        }

        internal void AddRule(string pattern)
        {
            try
            {
                FailRegex.TryAdd(pattern, new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(500)));
            }
            catch (Exception ex)
            {
                ex.Log(nameof(IPBanningModule), $"Invalid regex - '{pattern}'.");
            }
        }

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
        protected override Task OnRequestAsync(IHttpContext context)
        {
            ClientAddress = context.Request.RemoteEndPoint.Address;

            if (!Blacklist.ContainsKey(ClientAddress))
            {
                Task.Run(() =>
                {
                    AddRequest(ClientAddress);
                    UpdateRequestsBlackList();
                });

                return Task.CompletedTask;
            }

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
                _innerLogger.Dispose();
                while (!Whitelist.IsEmpty)
                {
                    Whitelist.TryTake(out _);
                }
            }

            _disposed = true;
        }

        private static void AddRequest(IPAddress address) =>
            Requests.GetOrAdd(address, new ConcurrentBag<long>()).Add(DateTime.Now.Ticks);
        
        private static void AddFailRegexMatch(IPAddress address) =>
            FailRegexMatches.GetOrAdd(address, new ConcurrentBag<long>()).Add(DateTime.Now.Ticks);

        private static void PurgeBlackList()
        {
            foreach (var k in Blacklist.Keys)
            {
                if (Blacklist.TryGetValue(k, out var info) &&
                    DateTime.Now.Ticks > info.ExpiresAt)
                    Blacklist.TryRemove(k, out _);
            }
        }

        private static void PurgeRequests()
        {
            var minTime = DateTime.Now.AddMinutes(-1).Ticks;
            foreach (var k in Requests.Keys)
            {
                if (Requests.TryGetValue(k, out var requests))
                {
                    var recentRequests = new ConcurrentBag<long>(requests.Where(x => x >= minTime));
                    if (!recentRequests.Any())
                        Requests.TryRemove(k, out _);
                    else
                        Requests.AddOrUpdate(k, recentRequests, (x, y) => recentRequests);
                }
            }
        }

        private static void PurgeFailRegexMatches()
        {
            var minTime = DateTime.Now.AddSeconds(-1 * SecondsMatchingPeriod).Ticks;
            foreach (var k in FailRegexMatches.Keys)
            {
                if (FailRegexMatches.TryGetValue(k, out var failRegexMatches))
                {
                    var recentMatches = new ConcurrentBag<long>(failRegexMatches.Where(x => x >= minTime));
                    if (!recentMatches.Any())
                        FailRegexMatches.TryRemove(k, out _);
                    else
                        FailRegexMatches.AddOrUpdate(k, recentMatches, (x, y) => recentMatches);
                }
            }
        }

        private void UpdateRequestsBlackList()
        {
            var lastSecond = DateTime.Now.AddSeconds(-1).Ticks;
            var lastMinute = DateTime.Now.AddMinutes(-1).Ticks;

            if (Requests.TryGetValue(ClientAddress, out var attempts) &&
                (attempts.Where(x => x >= lastSecond).Count() >= _maxRequestsPerSecond || 
                 (attempts.Where(x => x >= lastMinute).Count() / 60) >= _maxRequestsPerSecond))
                TryBanIP(ClientAddress, _banMinutes, false);
        }

        private void UpdateMatchBlackList()
        {
            var minTime = DateTime.Now.AddSeconds(-1 * SecondsMatchingPeriod).Ticks;
            if (FailRegexMatches.TryGetValue(ClientAddress, out var attempts) &&
                attempts.Where(x => x >= minTime).Count() >= _maxMatchCount)
                TryBanIP(ClientAddress, _banMinutes, false);
        }

        private class InnerIPBanningModuleLogger : ILogger
        {
            private bool _disposed;

            public InnerIPBanningModuleLogger(IPBanningModule parent)
            {
                Parent = parent;
                Logger.RegisterLogger(this);
            }

            /// <inheritdoc />
            public LogLevel LogLevel => LogLevel.Trace;

            private IPBanningModule Parent { get; set; }

            public void Dispose() =>
                Dispose(true);

            /// <inheritdoc />
            public void Log(LogMessageReceivedEventArgs logEvent)
            {
                // Process Log
                if (string.IsNullOrWhiteSpace(logEvent.Message) ||
                    Parent.ClientAddress == null ||
                    !FailRegex.Any() ||
                    Parent.Whitelist.Contains(Parent.ClientAddress) ||
                    Blacklist.ContainsKey(Parent.ClientAddress))
                    return;

                foreach (var regex in FailRegex.Values)
                {
                    try
                    {
                        if (!regex.IsMatch(logEvent.Message)) continue;

                        // Add to list
                        AddFailRegexMatch(Parent.ClientAddress);
                        Parent.UpdateMatchBlackList();
                        break;
                    }
                    catch (RegexMatchTimeoutException ex)
                    {
                        $"Timeout trying to match '{ex.Input}' with pattern '{ex.Pattern}'.".Error(nameof(InnerIPBanningModuleLogger));
                    }
                }
            }

            protected virtual void Dispose(bool disposing)
            {
                if (_disposed) return;
                if (disposing)
                {
                    Logger.UnregisterLogger(this);
                }

                _disposed = true;
            }
        }
    }
}
