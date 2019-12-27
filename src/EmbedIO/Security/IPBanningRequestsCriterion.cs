using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EmbedIO.Security
{
    public class IPBanningRequestsCriterion : IIPBanningCriterion
    {
        /// <summary>
        /// The default maximum request per second.
        /// </summary>
        public const int DefaultMaxRequestsPerSecond = 50;

        private static readonly ConcurrentDictionary<IPAddress, ConcurrentBag<long>> Requests = new ConcurrentDictionary<IPAddress, ConcurrentBag<long>>();

        private readonly int _maxRequestsPerSecond = DefaultMaxRequestsPerSecond;

        private static void AddRequest(IPAddress address) =>
            Requests.GetOrAdd(address, new ConcurrentBag<long>()).Add(DateTime.Now.Ticks);

        public Task UpdateBlacklist(IPAddress address)
        {
            var lastSecond = DateTime.Now.AddSeconds(-1).Ticks;
            var lastMinute = DateTime.Now.AddMinutes(-1).Ticks;

            if (Requests.TryGetValue(address, out var attempts) &&
                (attempts.Where(x => x >= lastSecond).Count() >= _maxRequestsPerSecond ||
                 (attempts.Where(x => x >= lastMinute).Count() / 60) >= _maxRequestsPerSecond))
                TryBanIP(address, _banMinutes, false);
        }

        public void PurgeData()
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
    }
}
