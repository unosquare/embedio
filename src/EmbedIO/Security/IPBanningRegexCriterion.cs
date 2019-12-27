using Swan;
using Swan.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EmbedIO.Security
{
    public class IPBanningRegexCriterion : IIPBanningCriterion
    {
        /// <summary>
        /// The default matching period.
        /// </summary>
        public const int DefaultSecondsMatchingPeriod = 60;

        /// <summary>
        /// The default maximum match count per period.
        /// </summary>
        public const int DefaultMaxMatchCount = 10;

        private static readonly ConcurrentDictionary<IPAddress, ConcurrentBag<long>> FailRegexMatches = new ConcurrentDictionary<IPAddress, ConcurrentBag<long>>();
        private static readonly ConcurrentDictionary<string, Regex> FailRegex = new ConcurrentDictionary<string, Regex>();

        private static int SecondsMatchingPeriod = DefaultSecondsMatchingPeriod;
        private readonly int _maxMatchCount;
        private ILogger _innerLogger;

        public IPBanningRegexCriterion()
        {
            _innerLogger = new InnerRegexCriterionLogger(this);
        }

        private static void AddFailRegexMatch(IPAddress address) =>
            FailRegexMatches.GetOrAdd(address, new ConcurrentBag<long>()).Add(DateTime.Now.Ticks);
        
        public Task UpdateData(IPAddress address)
        {
            var minTime = DateTime.Now.AddSeconds(-1 * SecondsMatchingPeriod).Ticks;
            if (FailRegexMatches.TryGetValue(address, out var attempts) &&
                attempts.Where(x => x >= minTime).Count() >= _maxMatchCount)
                TryBanIP(address, _banMinutes, false);
        }

        public void PurgeData()
        {
            var minTime = DateTime.Now.AddSeconds(-1 * SecondsMatchingPeriod).Ticks;
            foreach (var k in FailRegexMatches.Keys)
            {
                if (FailRegexMatches.TryGetValue(k, out var failRegexMatches))
                {
                    var recentMatches = new ConcurrentBag<long>(failRegexMatches.Where(x => x >= minTime));
                    if (!recentMatches.Any())
                        FailRegexMatches.TryRemove(k, out _);
                    else
                        FailRegexMatches.AddOrUpdate(k, recentMatches, (x, y) => recentMatches);
                }
            }
        }

        internal void AddRules(IEnumerable<string> patterns)
        {
            foreach (var pattern in patterns)
                AddRule(pattern);
        }

        internal void AddRule(string pattern)
        {
            try
            {
                FailRegex.TryAdd(pattern, new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(500)));
            }
            catch (Exception ex)
            {
                ex.Log(nameof(IPBanningModule), $"Invalid regex - '{pattern}'.");
            }
        }


        private class InnerRegexCriterionLogger : ILogger
        {
            private bool _disposed;

            public InnerRegexCriterionLogger(IPBanningRegexCriterion parent)
            {
                Parent = parent;
                Logger.RegisterLogger(this);
            }

            /// <inheritdoc />
            public LogLevel LogLevel => LogLevel.Trace;

            private IPBanningRegexCriterion Parent { get; set; }

            public void Dispose() =>
                Dispose(true);

            /// <inheritdoc />
            public void Log(LogMessageReceivedEventArgs logEvent)
            {
                // Process Log
                if (string.IsNullOrWhiteSpace(logEvent.Message) ||
                    Parent.ClientAddress == null ||
                    !FailRegex.Any() ||
                    Parent.Whitelist.Contains(Parent.ClientAddress) ||
                    Blacklist.ContainsKey(Parent.ClientAddress))
                    return;

                foreach (var regex in FailRegex.Values)
                {
                    try
                    {
                        if (!regex.IsMatch(logEvent.Message)) continue;

                        // Add to list
                        AddFailRegexMatch(Parent.ClientAddress);
                        Parent.UpdateBlacklist();
                        break;
                    }
                    catch (RegexMatchTimeoutException ex)
                    {
                        $"Timeout trying to match '{ex.Input}' with pattern '{ex.Pattern}'.".Error(nameof(InnerIPBanningModuleLogger));
                    }
                }
            }

            protected virtual void Dispose(bool disposing)
            {
                if (_disposed) return;
                if (disposing)
                {
                    Logger.UnregisterLogger(this);
                }

                _disposed = true;
            }
        }
    }
}
