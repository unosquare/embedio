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
        private static readonly ConcurrentDictionary<IPAddress, List<DateTime>> AccessAttempts = new ConcurrentDictionary<IPAddress, List<DateTime>>();
        private static readonly ConcurrentDictionary<IPAddress, BannedInfo> Blacklist = new ConcurrentDictionary<IPAddress, BannedInfo>();

        private readonly List<IPAddress> Whitelist = new List<IPAddress>();
        private readonly IEnumerable<string> failRegex;
        private readonly static int banTime = 30;
        private readonly int maxRetry = 10;
        private bool disposedValue = false;

        public IPBanningModule(string baseRoute)
            : base(baseRoute)
        {
            Logger.RegisterLogger(this);
        }

        public IPAddress? ClientAddress { get; set; }

        public override bool IsFinalHandler => false;

        public LogLevel LogLevel => LogLevel.Trace;

        public void Log(LogMessageReceivedEventArgs logEvent)
        {
            // Process Log
            if (ClientAddress == null ||
                failRegex?.Any() != true ||
                Whitelist.Contains(ClientAddress) ||
                Blacklist.ContainsKey(ClientAddress))
                return;

            foreach (var regex in failRegex)
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

        private void UpdateBlackList()
        {
            var time = DateTime.UtcNow.AddMinutes(-1 * banTime);
            if ((AccessAttempts[ClientAddress]?.Where(x => x >= time).Count() > maxRetry) == true)
            {
                TryBanIP(ClientAddress, banTime, false);
            }
        }

        protected override Task OnRequestAsync(IHttpContext context)
        {
            ClientAddress = context.Request.RemoteEndPoint.Address;
            PurgeBlackList();
            PurgeAccessAttempts(banTime);

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

        private static void PurgeAccessAttempts(int banTime)
        {
            var banDate = DateTime.UtcNow.AddMinutes(-1 * banTime);
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
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose() =>
            Dispose(true);
        
        #endregion

    }
}
