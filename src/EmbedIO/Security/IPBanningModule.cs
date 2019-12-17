using Swan;
using Swan.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EmbedIO.Security
{
    public class IPBanningModule : WebModuleBase, IDisposable, ILogger
    {
        public const int DefaultBanTime = 30;
        public const int DefaultMaxRetry = 10;

        private static readonly ConcurrentDictionary<IPAddress, List<DateTime>> AccessAttempts = new ConcurrentDictionary<IPAddress, List<DateTime>>();
        private static readonly ConcurrentDictionary<IPAddress, BannedInfo> Blacklist = new ConcurrentDictionary<IPAddress, BannedInfo>();

        private readonly List<IPAddress> _whitelist = new List<IPAddress>();
        private readonly IEnumerable<string>? _failRegex;
        private readonly int _banTime = 30;
        private readonly int _maxRetry = 10;
        private bool _disposedValue = false;

        public IPBanningModule(string baseRoute, IEnumerable<string> failRegex)
            : this(baseRoute, failRegex, null, DefaultBanTime, DefaultMaxRetry)
        {
        }

        public IPBanningModule(string baseRoute, IEnumerable<string> failRegex, IEnumerable<string>? whitelist)
            : this(baseRoute, failRegex, whitelist, DefaultBanTime, DefaultMaxRetry)
        {
        }

        public IPBanningModule(string baseRoute,
            IEnumerable<string> failRegex,
            int banTime)
            : this(baseRoute, failRegex, null, banTime, DefaultMaxRetry)
        {
        }

        public IPBanningModule(string baseRoute, 
            IEnumerable<string> failRegex, 
            IEnumerable<string>? whitelist,
            int banTime)
            : this(baseRoute, failRegex, whitelist, banTime, DefaultMaxRetry)
        {
        }

        public IPBanningModule(string baseRoute,
            IEnumerable<string> failRegex,
            int banTime,
            int maxRerty)
            : this(baseRoute, failRegex, null, banTime, maxRerty)
        {
        }

        public IPBanningModule(string baseRoute,
            IEnumerable<string> failRegex,
            IEnumerable<string>? whitelist,
            int banTime,
            int maxRerty)
            : base(baseRoute)
        {
            _failRegex = failRegex;
            _banTime = banTime;
            _maxRetry = maxRerty;

            ParseWhiteList(whitelist);

            Logger.RegisterLogger(this);
        }

        public IPAddress? ClientAddress { get; set; }

        public override bool IsFinalHandler => false;

        public LogLevel LogLevel => LogLevel.Trace;

        public void Log(LogMessageReceivedEventArgs logEvent)
        {
            // Process Log
            if (ClientAddress == null ||
                _failRegex?.Any() != true ||
                _whitelist.Contains(ClientAddress) ||
                Blacklist.ContainsKey(ClientAddress))
                return;

            foreach (var regex in _failRegex)
            {
                if (Regex.IsMatch(logEvent.Message, regex, RegexOptions.CultureInvariant))
                {
                    // Add to list
                    AddAccessAttempt(ClientAddress);
                    UpdateBlackList();
                    break;
                }
            }
        }

        protected override Task OnRequestAsync(IHttpContext context)
        {
            ClientAddress = context.Request.RemoteEndPoint.Address;
            PurgeBlackList();
            PurgeAccessAttempts();

            if (Blacklist.ContainsKey(ClientAddress))
            {
                context.SetHandled();
                throw HttpException.Forbidden();
            }

            return Task.CompletedTask;
        }

        public static IEnumerable<BannedInfo> GetBannedIPs()
        {
            PurgeBlackList();
            return Blacklist.Values.ToList();
        }

        private void ParseWhiteList(IEnumerable<string>? whitelist)
        {
            if (whitelist?.Any() != true)
                return;

            foreach (var address in whitelist)
            {
                var ipAdresses = ParseIPAddress(address);
                foreach (var ipAddress in ipAdresses)
                {
                    if (!_whitelist.Contains(ipAddress))
                        _whitelist.Add(ipAddress);
                }
            }
        }

        private IEnumerable<IPAddress> ParseIPAddress(string address)
        {
            // TODO:
            return new List<IPAddress>();
        }

        private void UpdateBlackList()
        {
            var time = DateTime.UtcNow.AddMinutes(-1);
            if ((AccessAttempts[ClientAddress]?.Where(x => x >= time).Count() > _maxRetry) == true)
            {
                TryBanIP(ClientAddress, _banTime, false);
            }
        }

        public static bool TryBanIP(IPAddress address, int minutes, bool isExplicit = true) =>
            TryBanIP(address, DateTime.UtcNow.AddMinutes(minutes), isExplicit);

        public static bool TryBanIP(IPAddress address, TimeSpan banTime, bool isExplicit = true) =>
            TryBanIP(address, DateTime.UtcNow.Add(banTime), isExplicit);

        public static bool TryBanIP(IPAddress address, DateTime banUntil, bool isExplicit = true)
        {
            if (Blacklist.ContainsKey(address))
            {
                var bannedInfo = Blacklist[address];
                bannedInfo.BanUntil = banUntil;
                bannedInfo.IsExplicit = isExplicit;

                return true;
            }

            return Blacklist.TryAdd(address, new BannedInfo()
            {
                IPAddress = address,
                BanUntil = banUntil,
                IsExplicit = isExplicit,
            });
        }

        public static bool TryUnbanIP(IPAddress address) =>
            Blacklist.TryRemove(address, out _);

        private static void AddAccessAttempt(IPAddress address)
        {
            if (AccessAttempts.ContainsKey(address))
                AccessAttempts[address].Add(DateTime.UtcNow);
            else
                AccessAttempts.TryAdd(address, new List<DateTime>() { DateTime.UtcNow });
        }

        private static void PurgeBlackList()
        {
            var keys = Blacklist.Keys;

            foreach (var k in keys)
            {
                if (DateTime.UtcNow > Blacklist[k].BanUntil)
                    Blacklist.TryRemove(k, out _);
            }
        }

        private static void PurgeAccessAttempts()
        {
            var banDate = DateTime.UtcNow.AddMinutes(-1);
            var keys = AccessAttempts.Keys;

            foreach (var k in keys)
            {
                AccessAttempts[k] = AccessAttempts[k].Where(x => x >= banDate).ToList();
                if (!AccessAttempts[k].Any())
                    AccessAttempts.TryRemove(k, out _);
            }
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _whitelist?.Clear();
                }

                _disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose() =>
            Dispose(true);
        
        #endregion

    }
}
