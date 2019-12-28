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
    public class IPBanningConfiguration : ConfiguredObject
    {
        private readonly List<IIPBanningCriterion> _criterions = new List<IIPBanningCriterion>();
        private readonly ConcurrentDictionary<IPAddress, BanInfo> _blacklistDictionary = new ConcurrentDictionary<IPAddress, BanInfo>();
        private readonly ConcurrentBag<IPAddress> _whiteListBag = new ConcurrentBag<IPAddress>();
        private int _banTime;

        public int BanTime
        {
            get => _banTime;
            set
            {
                EnsureConfigurationNotLocked();
                _banTime = value;
            }
        }
        
        public List<BanInfo> BlackList => _blacklistDictionary.Values.ToList();

        public void RegisterCriterion(IIPBanningCriterion criterion)
        {
            EnsureConfigurationNotLocked();
            _criterions.Add(criterion);
        }


        public void Purge()
        {
            PurgeBlackList();
            foreach (var criterion in _criterions)
            {
                criterion.PurgeData();
            }
        }

        public void PurgeBlackList()
        {
            foreach (var k in _blacklistDictionary.Keys)
            {
                if (_blacklistDictionary.TryGetValue(k, out var info) &&
                    DateTime.Now.Ticks > info.ExpiresAt)
                    _blacklistDictionary.TryRemove(k, out _);
            }
        }

        public void Lock() => LockConfiguration();

        public void AddOrUpdateBlackList(IPAddress address, Func<IPAddress, BanInfo> addValueFactory, Func<IPAddress, BanInfo, BanInfo> updateValueFactory)
            => _blacklistDictionary.AddOrUpdate(address, addValueFactory, updateValueFactory);

        internal async Task AddToWhitelistAsync(IEnumerable<string> whitelist)
        {
            if (whitelist?.Any() != true)
                return;

            foreach (var address in whitelist)
            {
                var addressees = await IPParser.ParseAsync(address).ConfigureAwait(false);

                foreach (var ipAddress in addressees.Where(x => !_whiteListBag.Contains(x)))
                {
                    _whiteListBag.Add(ipAddress);
                }
            }
        }

        public async Task CheckClient(IPAddress clientAddress)
        {
            if (_whiteListBag.Contains(clientAddress))
                return;

            foreach (var criterion in _criterions)
            {
                await criterion.UpdateData(clientAddress);
            }

            if (_blacklistDictionary.ContainsKey(clientAddress))
                throw HttpException.Forbidden();
        }

        public bool TryRemoveBlackList(IPAddress address) => _blacklistDictionary.TryRemove(address, out _);
    }
}