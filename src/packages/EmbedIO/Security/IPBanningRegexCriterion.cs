using Swan.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EmbedIO.Security
{
    /// <summary>
    /// Represents a log message regex matching criterion for <see cref="IPBanningModule"/>.
    /// </summary>
    /// <seealso cref="IIPBanningCriterion" />
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

        private readonly ConcurrentDictionary<IPAddress, ConcurrentBag<long>> _failRegexMatches = new ConcurrentDictionary<IPAddress, ConcurrentBag<long>>();
        private readonly ConcurrentDictionary<string, Regex> _failRegex = new ConcurrentDictionary<string, Regex>();
        private readonly IPBanningModule _parent;
        private readonly int _secondsMatchingPeriod;
        private readonly int _maxMatchCount;
        private readonly ILogger? _innerLogger;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="IPBanningRegexCriterion"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="rules">The rules.</param>
        /// <param name="maxMatchCount">The maximum match count.</param>
        /// <param name="secondsMatchingPeriod">The seconds matching period.</param>
        public IPBanningRegexCriterion(IPBanningModule parent, IEnumerable<string> rules, int maxMatchCount = DefaultMaxMatchCount, int secondsMatchingPeriod = DefaultSecondsMatchingPeriod)
        {
            _secondsMatchingPeriod = secondsMatchingPeriod;
            _maxMatchCount = maxMatchCount;
            _parent = parent;

            AddRules(rules);

            if (_failRegex.Any())
                _innerLogger = new InnerRegexCriterionLogger(this);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="IPBanningRegexCriterion"/> class.
        /// </summary>
        ~IPBanningRegexCriterion()
        {
            Dispose(false);
        }

        /// <inheritdoc />
        public Task<bool> ValidateIPAddress(IPAddress address)
        {
            var minTime = DateTime.Now.AddSeconds(-1 * _secondsMatchingPeriod).Ticks;
            var shouldBan = _failRegexMatches.TryGetValue(address, out var attempts) &&
                            attempts.Count(x => x >= minTime) >= _maxMatchCount;

            return Task.FromResult(shouldBan);
        }

        /// <inheritdoc />
        public void ClearIPAddress(IPAddress address) =>
            _failRegexMatches.TryRemove(address, out _);

        /// <inheritdoc />
        public void PurgeData()
        {
            var minTime = DateTime.Now.AddSeconds(-1 * _secondsMatchingPeriod).Ticks;

            foreach (var k in _failRegexMatches.Keys)
            {
                if (!_failRegexMatches.TryGetValue(k, out var failRegexMatches)) continue;

                var recentMatches = new ConcurrentBag<long>(failRegexMatches.Where(x => x >= minTime));
                if (!recentMatches.Any())
                    _failRegexMatches.TryRemove(k, out _);
                else
                    _failRegexMatches.AddOrUpdate(k, recentMatches, (x, y) => recentMatches);
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
            if (_disposed)
                return;

            if (disposing)
            {
                _failRegexMatches.Clear();
                _failRegex.Clear();
                if (_innerLogger != null)
                {
                    try
                    {
                        Logger.UnregisterLogger(_innerLogger);
                    }
                    catch
                    {
                        // ignore
                    }

                    _innerLogger.Dispose();
                }
            }

            _disposed = true;
        }

        private void MatchIP(IPAddress address, string message)
        {
            if (!_parent.Configuration.ShouldContinue(address))
                return;

            foreach (var regex in _failRegex.Values)
            {
                try
                {
                    if (!regex.IsMatch(message)) continue;

                    _failRegexMatches.GetOrAdd(address, new ConcurrentBag<long>()).Add(DateTime.Now.Ticks);
                    break;
                }
                catch (RegexMatchTimeoutException ex)
                {
                    $"Timeout trying to match '{ex.Input}' with pattern '{ex.Pattern}'.".Error(nameof(InnerRegexCriterionLogger));
                }
            }
        }

        private void AddRules(IEnumerable<string> patterns)
        {
            foreach (var pattern in patterns)
                AddRule(pattern);
        }

        private void AddRule(string pattern)
        {
            try
            {
                _failRegex.TryAdd(pattern, new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(500)));
            }
            catch (Exception ex)
            {
                ex.Log(nameof(IPBanningModule), $"Invalid regex - '{pattern}'.");
            }
        }

        private sealed class InnerRegexCriterionLogger : ILogger
        {
            private readonly IPBanningRegexCriterion _parent;

            public InnerRegexCriterionLogger(IPBanningRegexCriterion parent)
            {
                _parent = parent;
                Logger.RegisterLogger(this);
            }

            /// <inheritdoc />
            public LogLevel LogLevel => LogLevel.Trace;

            public void Dispose() 
            { 
                // DO nothing
            }

            /// <inheritdoc />
            public void Log(LogMessageReceivedEventArgs logEvent)
            {
                var clientAddress = _parent._parent.ClientAddress;

                if (clientAddress == null || string.IsNullOrWhiteSpace(logEvent.Message))
                    return;

                _parent.MatchIP(clientAddress, logEvent.Message);
            }
        }
    }
}
