﻿using EmbedIO.Utilities;
using Swan;
using Swan.Logging;
using Swan.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO.Security
{
    /// <summary>
    /// A module for ban IPs that show the malicious signs, based on scanning log messages.
    /// </summary>
    /// <seealso cref="WebModuleBase" />
    public class IPBanningModule : WebModuleBase, IDisposable, ILogger
    {
        /// <summary>
        /// The default ban time, in minutes.
        /// </summary>
        public const int DefaultBanTime = 30;

        /// <summary>
        /// The default maximum retries per minute.
        /// </summary>
        public const int DefaultMaxRetry = 10;

        private static readonly ConcurrentDictionary<IPAddress, ConcurrentBag<long>> AccessAttempts = new ConcurrentDictionary<IPAddress, ConcurrentBag<long>>();
        private static readonly ConcurrentDictionary<IPAddress, BannedInfo> Blacklist = new ConcurrentDictionary<IPAddress, BannedInfo>();
        private static readonly ConcurrentDictionary<string, Regex> FailRegex = new ConcurrentDictionary<string, Regex>();

        private readonly List<IPAddress> _whitelist = new List<IPAddress>();
        private readonly int _banTime = 30;
        private readonly int _maxRetry = 10;
        private bool _disposedValue = false;
        private PeriodicTask? _purger;

        /// <summary>
        /// Initializes a new instance of the <see cref="IPBanningModule"/> class.
        /// </summary>
        /// <param name="baseRoute">The base route.</param>
        /// <param name="failRegex">A collection of regex to match the log messages against.</param>
        /// <param name="banTime">The time that an IP will remain ban, in minutes.</param>
        /// <param name="maxRetry">The maximum number of failed attempts before banning an IP.</param>
        public IPBanningModule(string baseRoute,
            IEnumerable<string> failRegex,
            int banTime = DefaultBanTime,
            int maxRetry = DefaultMaxRetry)
            : this(baseRoute, failRegex, null, banTime, maxRetry)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IPBanningModule"/> class.
        /// </summary>
        /// <param name="baseRoute">The base route.</param>
        /// <param name="failRegex">A collection of regex to match the log messages against.</param>
        /// <param name="whitelist">A collection of valid IPs that never will be banned.</param>
        /// <param name="banTime">The time that an IP will remain ban, in minutes.</param>
        /// <param name="maxRetry">The maximum number of failed attempts before banning an IP.</param>
        public IPBanningModule(string baseRoute,
            IEnumerable<string> failRegex,
            IEnumerable<string>? whitelist = null,
            int banTime = DefaultBanTime,
            int maxRetry = DefaultMaxRetry)
            : base(baseRoute)
        {
            foreach (var pattern in failRegex)
            {
                try
                {
                    FailRegex.TryAdd(pattern, new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(500)));
                }
                catch (Exception ex)
                {
                    ex.Log(nameof(IPBanningModule));
                }
            }

            _banTime = banTime;
            _maxRetry = maxRetry;

            ParseWhiteList(whitelist);

            _purger = new PeriodicTask(TimeSpan.FromMinutes(1), ct =>
            {
                PurgeBlackList();
                PurgeAccessAttempts();

                return Task.CompletedTask;
            });

            Logger.RegisterLogger(this);
        }

        /// <inheritdoc />
        public override bool IsFinalHandler => false;

        /// <inheritdoc />
        public LogLevel LogLevel => LogLevel.Trace;

        private IPAddress? ClientAddress { get; set; }

        /// <summary>
        /// Gets the list of current banned IPs.
        /// </summary>
        /// <returns>A collection of <see cref="BannedInfo"/> in the blacklist.</returns>
        public static IEnumerable<BannedInfo> GetBannedIPs() =>
            Blacklist.Values.ToList();

        /// <summary>
        /// Tries to ban an IP explicitly.
        /// </summary>
        /// <param name="address">The IP address to ban.</param>
        /// <param name="minutes">The time in minutes that the IP will remain ban.</param>
        /// <param name="isExplicit">if set to <c>true</c> [is explicit].</param>
        /// <returns>
        ///     <c>true</c> if the IP was added to the blacklist; otherwise, <c>false</c>.
        /// </returns>
        public static bool TryBanIP(IPAddress address, int minutes, bool isExplicit = true) =>
            TryBanIP(address, DateTime.Now.AddMinutes(minutes), isExplicit);

        /// <summary>
        /// Tries to ban an IP explicitly.
        /// </summary>
        /// <param name="address">The IP address to ban.</param>
        /// <param name="banTime">An <see cref="TimeSpan"/> that sets the time the IP will remain ban.</param>
        /// <param name="isExplicit">if set to <c>true</c> [is explicit].</param>
        /// <returns>
        ///     <c>true</c> if the IP was added to the blacklist; otherwise, <c>false</c>.
        /// </returns>
        public static bool TryBanIP(IPAddress address, TimeSpan banTime, bool isExplicit = true) =>
            TryBanIP(address, DateTime.Now.Add(banTime), isExplicit);

        /// <summary>
        /// Tries to ban an IP explicitly.
        /// </summary>
        /// <param name="address">The IP address to ban.</param>
        /// <param name="banUntil">A <see cref="DateTime"/> that sets until when the IP will remain ban.</param>
        /// <param name="isExplicit">if set to <c>true</c> [is explicit].</param>
        /// <returns>
        ///     <c>true</c> if the IP was added to the blacklist; otherwise, <c>false</c>.
        /// </returns>
        public static bool TryBanIP(IPAddress address, DateTime banUntil, bool isExplicit = true)
        {
            if (Blacklist.ContainsKey(address))
            {
                var bannedInfo = Blacklist[address];
                bannedInfo.BanUntil = banUntil.Ticks;
                bannedInfo.IsExplicit = isExplicit;

                return true;
            }

            return Blacklist.TryAdd(address, new BannedInfo()
            {
                IPAddress = address,
                BanUntil = banUntil.Ticks,
                IsExplicit = isExplicit,
            });
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
        public void Log(LogMessageReceivedEventArgs logEvent)
        {
            // Process Log
            if (string.IsNullOrWhiteSpace(logEvent.Message) ||
                ClientAddress == null ||
                !FailRegex.Any() ||
                _whitelist.Contains(ClientAddress) ||
                Blacklist.ContainsKey(ClientAddress))
                return;

            foreach (var regex in FailRegex.Values)
            {
                try
                {
                    if (regex.IsMatch(logEvent.Message))
                    {
                        // Add to list
                        AddAccessAttempt(ClientAddress);
                        UpdateBlackList();
                        break;
                    }
                }
                catch (RegexMatchTimeoutException ex)
                {
                    $"Timeout trying to match '{ex.Input}' with pattern '{ex.Pattern}'.".Error(nameof(IPBanningModule));
                }
            }
        }

        /// <inheritdoc />
        protected override Task OnRequestAsync(IHttpContext context)
        {
            ClientAddress = context.Request.RemoteEndPoint.Address;
            if (Blacklist.ContainsKey(ClientAddress))
            {
                context.SetHandled();
                throw HttpException.Forbidden();
            }

            return Task.CompletedTask;
        }

        private static void AddAccessAttempt(IPAddress address)
        {
            if (AccessAttempts.ContainsKey(address))
                AccessAttempts[address].Add(DateTime.Now.Ticks);
            else
                AccessAttempts.TryAdd(address, new ConcurrentBag<long>() { DateTime.Now.Ticks });
        }

        private static void PurgeBlackList()
        {
            foreach (var k in Blacklist.Keys)
            {
                if (DateTime.Now.Ticks > Blacklist[k].BanUntil)
                    Blacklist.TryRemove(k, out _);
            }
        }

        private static void PurgeAccessAttempts()
        {
            var banDate = DateTime.Now.AddMinutes(-1).Ticks;
            
            foreach (var k in AccessAttempts.Keys)
            {
                var recentAttempts = new ConcurrentBag<long>(AccessAttempts[k].Where(x => x >= banDate));
                if (!recentAttempts.Any())
                    AccessAttempts.TryRemove(k, out _);
                else
                    Interlocked.Exchange(ref recentAttempts, AccessAttempts[k]);
            }
        }

        private void ParseWhiteList(IEnumerable<string>? whitelist)
        {
            if (whitelist?.Any() != true)
                return;

            foreach (var address in whitelist)
            {
                var adresses = IPParser.Parse(address);
                foreach (var ipAddress in adresses)
                {
                    if (!_whitelist.Contains(ipAddress))
                        _whitelist.Add(ipAddress);
                }
            }
        }

        private void UpdateBlackList()
        {
            var time = DateTime.Now.AddMinutes(-1).Ticks;
            if ((AccessAttempts[ClientAddress]?.Where(x => x >= time).Count() > _maxRetry) == true)
            {
                TryBanIP(ClientAddress, _banTime, false);
            }
        }

        #region IDisposable Support

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _whitelist.Clear();
                    _purger?.Dispose();
                }

                _purger = null;
                _disposedValue = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() =>
            Dispose(true);
        
        #endregion
    }
}
