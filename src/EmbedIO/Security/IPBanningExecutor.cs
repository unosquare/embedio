using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Swan.Threading;

namespace EmbedIO.Security
{
    public static class IPBanningExecutor
    {
        private static readonly ConcurrentDictionary<string, IPBanningConfiguration> Configurations = new ConcurrentDictionary<string, IPBanningConfiguration>();
        private static readonly PeriodicTask? Purger;

        static IPBanningExecutor()
        {
            Purger = new PeriodicTask(TimeSpan.FromMinutes(1), ct =>
            {
                foreach (var conf in Configurations.Keys)
                {
                    if (Configurations.TryGetValue(conf, out var instance))
                        instance.Purge();
                }

                return Task.CompletedTask;
            });
        }

        public static IPBanningConfiguration RetrieveInstance(string baseRoute) => Configurations.GetOrAdd(baseRoute, (x) => new IPBanningConfiguration());

        public static bool TryGetInstance(string baseRoute, out IPBanningConfiguration configuration) => Configurations.TryGetValue(baseRoute, out configuration);
    }
}