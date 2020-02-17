using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Swan.Threading;

namespace EmbedIO.Security.Internal
{
    internal static class IPBanningExecutor
    {
        private static readonly ConcurrentDictionary<string, IPBanningConfiguration> Configurations = new ConcurrentDictionary<string, IPBanningConfiguration>();

        private static readonly PeriodicTask Purger = new PeriodicTask(TimeSpan.FromMinutes(1), ct => {
            foreach (var conf in Configurations.Keys)
            {
                if (Configurations.TryGetValue(conf, out var instance))
                    instance.Purge();
            }

            return Task.CompletedTask;
        });

        public static IPBanningConfiguration RetrieveInstance(string baseRoute, int banMinutes) => 
            Configurations.GetOrAdd(baseRoute, x => new IPBanningConfiguration(banMinutes));

        public static bool TryGetInstance(string baseRoute, out IPBanningConfiguration configuration) => 
            Configurations.TryGetValue(baseRoute, out configuration);

        public static bool TryRemoveInstance(string baseRoute) =>
            Configurations.TryRemove(baseRoute, out _);
    }
}