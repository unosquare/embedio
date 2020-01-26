using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace EmbedIO.Security
{
    /// <summary>
    /// Represents a maximun requests per second criterion for <see cref="IPBanningModule"/>.
    /// </summary>
    /// <seealso cref="IIPBanningCriterion" />
    public class IPBanningRequestsCriterion : IIPBanningCriterion
    {
        /// <summary>
        /// The default maximum request per second.
        /// </summary>
        public const int DefaultMaxRequestsPerSecond = 50;

        private static readonly ConcurrentDictionary<IPAddress, ConcurrentBag<long>> Requests = new ConcurrentDictionary<IPAddress, ConcurrentBag<long>>();

        private readonly int _maxRequestsPerSecond;

        private bool _disposed;

        internal IPBanningRequestsCriterion(int maxRequestsPerSecond)
        {
            _maxRequestsPerSecond = maxRequestsPerSecond;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="IPBanningRequestsCriterion"/> class.
        /// </summary>
        ~IPBanningRequestsCriterion()
        {
            Dispose(false);
        }

        /// <inheritdoc />
        public Task<bool> ValidateIPAddress(IPAddress address)
        {
            Requests.GetOrAdd(address, new ConcurrentBag<long>()).Add(DateTime.Now.Ticks);

            var lastSecond = DateTime.Now.AddSeconds(-1).Ticks;
            var lastMinute = DateTime.Now.AddMinutes(-1).Ticks;

            var shouldBan = Requests.TryGetValue(address, out var attempts) &&
                (attempts.Count(x => x >= lastSecond) >= _maxRequestsPerSecond ||
                 (attempts.Count(x => x >= lastMinute) / 60) >= _maxRequestsPerSecond);

            return Task.FromResult(shouldBan);
        }

        /// <inheritdoc />
        public void ClearIPAddress(IPAddress address) =>
            Requests.TryRemove(address, out _);

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

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                Requests.Clear();
            }

            _disposed = true;
        }
    }
}
