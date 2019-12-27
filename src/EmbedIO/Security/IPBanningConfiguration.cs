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
        private readonly ConcurrentDictionary<IPAddress, BanInfo> BlacklistDictionary = new ConcurrentDictionary<IPAddress, BanInfo>();
        private readonly ConcurrentBag<IPAddress> WhiteListBag = new ConcurrentBag<IPAddress>();
        private readonly int _banTime;

        public int BanTime
        {
            get => _banTime;
            set
            {
                EnsureConfigurationNotLocked();
                _banTime = value;
            }
        }

        public void RegisterCriterion(IIPBanningCriterion criterion)
        {
            EnsureConfigurationNotLocked();
            _criterions.Add(criterion);
        }

        public List<BanInfo> BlackList => BlacklistDictionary.Values.ToList();

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
            foreach (var k in BlacklistDictionary.Keys)
            {
                if (BlacklistDictionary.TryGetValue(k, out var info) &&
                    DateTime.Now.Ticks > info.ExpiresAt)
                    BlacklistDictionary.TryRemove(k, out _);
            }
        }

        public void Lock() => LockConfiguration();

        public void AddOrUpdateBlackList(IPAddress address, Func<IPAddress, BanInfo> addValueFactory, Func<IPAddress, BanInfo, BanInfo> updateValueFactory)
            => BlacklistDictionary.AddOrUpdate(address, addValueFactory, updateValueFactory);

        internal async Task AddToWhitelistAsync(IEnumerable<string> whitelist)
        {
            if (whitelist?.Any() != true)
                return;

            foreach (var address in whitelist)
            {
                var addressees = await IPParser.ParseAsync(address).ConfigureAwait(false);

                foreach (var ipAddress in addressees.Where(x => !WhiteListBag.Contains(x)))
                {
                    WhiteListBag.Add(ipAddress);
                }
            }
        }

        public async Task CheckClient(IPAddress clientAddress)
        {
            if (WhiteListBag.Contains(clientAddress))
                return;

            foreach (var criterion in _criterions)
            {
                await criterion.UpdateData(clientAddress);
            }

            if (BlacklistDictionary.ContainsKey(clientAddress))
                throw HttpException.Forbidden();
        }

        public bool TryRemoveBlackList(IPAddress address) => BlacklistDictionary.TryRemove(address, out _);
    }
}