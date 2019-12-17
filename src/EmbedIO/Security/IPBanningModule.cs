using Swan;
using Swan.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace EmbedIO.Security
{
    public class IPBanningModule : WebModuleBase, IDisposable, ILogger
    {
        private static readonly ConcurrentDictionary<IPAddress, BannedInfo> Blacklist = new ConcurrentDictionary<IPAddress, BannedInfo>();
        
        private bool disposedValue = false;
        
        public IPBanningModule(string baseRoute)
            : base(baseRoute)
        {
            Logger.RegisterLogger(this);
        }

        public override bool IsFinalHandler => false;

        public static int BanTime { get; private set; } = 30;

        public LogLevel LogLevel => LogLevel.Trace;

        public void Log(LogMessageReceivedEventArgs logEvent)
        {
            // Process Log

        }

        protected override Task OnRequestAsync(IHttpContext context)
        {
            PurgeBlackList();

            var remoteAddress = context.Request.RemoteEndPoint.Address;
            if (Blacklist.ContainsKey(remoteAddress))
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

        public static bool TryBanIP(IPAddress address) =>
            TryBanIP(address, DateTime.UtcNow.AddMinutes(BanTime));

        public static bool TryBanIP(IPAddress address, int minutes) =>
            TryBanIP(address, DateTime.UtcNow.AddMinutes(minutes));

        public static bool TryBanIP(IPAddress address, TimeSpan banTime) =>
            TryBanIP(address, DateTime.UtcNow.Add(banTime));

        public static bool TryBanIP(IPAddress address, DateTime banUntil)
        {
            if (Blacklist.ContainsKey(address))
            {
                var bannedInfo = Blacklist[address];
                bannedInfo.BanUntil = banUntil;
                bannedInfo.IsExplicit = true;

                return true;
            }

            return Blacklist.TryAdd(address, new BannedInfo()
            {
                IPAddress = address,
                BanUntil = banUntil,
                IsExplicit = true,
            });
        }

        public static bool TryUnbanIP(IPAddress ipAddress) =>
            Blacklist.TryRemove(ipAddress, out _);

        private static void PurgeBlackList() =>
            Blacklist.ForEach((k, v) =>
            {
                if (DateTime.UtcNow > v.BanUntil)
                    Blacklist.TryRemove(k, out _);
            });
        
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
