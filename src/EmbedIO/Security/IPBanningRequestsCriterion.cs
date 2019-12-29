using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
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

        private readonly int _maxRequestsPerSecond;

        internal IPBanningRequestsCriterion(int maxRequestsPerSecond)
        {
            _maxRequestsPerSecond = maxRequestsPerSecond;
        }

        /// <inheritdoc />
        public Task<bool> ValidateIPAddress(IPAddress address)
        {
            var lastSecond = DateTime.Now.AddSeconds(-1).Ticks;
            var lastMinute = DateTime.Now.AddMinutes(-1).Ticks;

            var shouldBan = Requests.TryGetValue(address, out var attempts) &&
                (attempts.Count(x => x >= lastSecond) >= _maxRequestsPerSecond ||
                 (attempts.Count(x => x >= lastMinute) / 60) >= _maxRequestsPerSecond);

            return Task.FromResult(shouldBan);
        }

        /// <inheritdoc />
        public void PurgeData()
        {
            var minTime = DateTime.Now.AddMinutes(-1).Ticks;

            foreach (var k in Requests.Keys)
            {
                if (!Requests.TryGetValue(k, out var requests)) continue;

                var recentRequests = new ConcurrentBag<long>(requests.Where(x => x >= minTime));
                if (!recentRequests.Any())
                    Requests.TryRemove(k, out _);
                else
                    Requests.AddOrUpdate(k, recentRequests, (x, y) => recentRequests);
            }
        }
        
        private static void AddRequest(IPAddress address) =>
            Requests.GetOrAdd(address, new ConcurrentBag<long>()).Add(DateTime.Now.Ticks);
    }
}
