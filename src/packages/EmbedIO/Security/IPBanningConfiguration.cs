using EmbedIO.Utilities;
using Swan.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace EmbedIO.Security
{
    /// <summary>
    /// Represents a configuration object for <see cref="IPBanningModule"/>.
    /// </summary>
    /// <seealso cref="ConfiguredObject" />
    public class IPBanningConfiguration : ConfiguredObject, IDisposable
    {
        private readonly List<IIPBanningCriterion> _criterions = new List<IIPBanningCriterion>();
        private readonly ConcurrentDictionary<IPAddress, BanInfo> _blacklistDictionary = new ConcurrentDictionary<IPAddress, BanInfo>();
        private readonly ConcurrentBag<IPAddress> _whiteListBag = new ConcurrentBag<IPAddress>();
        private readonly int _banTime;
        private bool _disposed;

        internal IPBanningConfiguration(int banTime)
        {
            _banTime = banTime;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="IPBanningConfiguration"/> class.
        /// </summary>
        ~IPBanningConfiguration()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets the black list.
        /// </summary>
        /// <value>
        /// The black list.
        /// </value>
        public List<BanInfo> BlackList => _blacklistDictionary.Values.ToList();

        /// <summary>
        /// Check if a Criterion should continue testing an IP Address.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <returns><c>true</c> if the Criterion should continue, otherwise <c>false</c>.</returns>
        public bool ShouldContinue(IPAddress address) => 
            !_whiteListBag.Contains(address) || !_blacklistDictionary.ContainsKey(address);

        /// <summary>
        /// Purges this instance.
        /// </summary>
        public void Purge()
        {
            PurgeBlackList();

            foreach (var criterion in _criterions)
            {
                criterion.PurgeData();
            }
        }

        /// <summary>
        /// Checks the client.
        /// </summary>
        /// <param name="clientAddress">The client address.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task CheckClient(IPAddress clientAddress)
        {
            if (_whiteListBag.Contains(clientAddress))
                return;

            foreach (var criterion in _criterions)
            {
                var result = await criterion.ValidateIPAddress(clientAddress).ConfigureAwait(false);

                if (!result) continue;

                TryBanIP(clientAddress, false);
                break;
            }

            if (_blacklistDictionary.ContainsKey(clientAddress))
                throw HttpException.Forbidden();
        }
        
        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal async Task AddToWhitelistAsync(IEnumerable<string>? whitelist)
        {
            if (whitelist?.Any() != true)
                return;

            foreach (var whiteAddress in whitelist)
            {
                var parsedAddresses = await IPParser.ParseAsync(whiteAddress).ConfigureAwait(false);
                foreach (var address in parsedAddresses.Where(x => !_whiteListBag.Contains(x)))
                {
                    _whiteListBag.Add(address);
                }
            }
        }
        
        internal void Lock() => LockConfiguration();

        internal bool TryRemoveBlackList(IPAddress address)
        {
            foreach (var criterion in _criterions)
            {
                criterion.ClearIPAddress(address);
            }

            return _blacklistDictionary.TryRemove(address, out _);
        }

        internal void RegisterCriterion(IIPBanningCriterion criterion)
        {
            EnsureConfigurationNotLocked();
            _criterions.Add(criterion);
        }

        internal bool TryBanIP(IPAddress address, bool isExplicit, DateTime? banUntil = null)
        {
            try
            {
                _blacklistDictionary.AddOrUpdate(address,
                    k =>
                        new BanInfo
                        {
                            IPAddress = k,
                            ExpiresAt = banUntil?.Ticks ?? DateTime.Now.AddMinutes(_banTime).Ticks,
                            IsExplicit = isExplicit,
                        },
                    (k, v) =>
                        new BanInfo
                        {
                            IPAddress = k,
                            ExpiresAt = banUntil?.Ticks ?? DateTime.Now.AddMinutes(_banTime).Ticks,
                            IsExplicit = isExplicit,
                        });

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _blacklistDictionary.Clear();

                _criterions.ForEach(x => x.Dispose());
                _criterions.Clear();
            }

            _disposed = true;
        }

        private void PurgeBlackList()
        {
            foreach (var k in _blacklistDictionary.Keys)
            {
                if (_blacklistDictionary.TryGetValue(k, out var info) &&
                    DateTime.Now.Ticks > info.ExpiresAt)
                    _blacklistDictionary.TryRemove(k, out _);
            }
        }
    }
}