using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Internal;
using Swan.Threading;
using Swan.Logging;

namespace EmbedIO.Files
{
#pragma warning disable CA1001 // Type owns disposable field '_cleaner' but is not disposable - _cleaner has its own dispose semantics.
    /// <summary>
    /// A cache where one or more instances of <see cref="FileModule"/> can store hashes and file contents.
    /// </summary>
    public sealed partial class FileCache
#pragma warning restore CA1001
    {
        /// <summary>
        /// The default value for the <see cref="MaxSizeKb"/> property.
        /// </summary>
        public const int DefaultMaxSizeKb = 10240;

        /// <summary>
        /// The default value for the <see cref="MaxFileSizeKb"/> property.
        /// </summary>
        public const int DefaultMaxFileSizeKb = 200;

        private static readonly Stopwatch TimeBase = Stopwatch.StartNew();

        private static readonly object DefaultSyncRoot = new object();
        private static FileCache? _defaultInstance;

        private readonly ConcurrentDictionary<string, Section> _sections = new ConcurrentDictionary<string, Section>(StringComparer.Ordinal);
        private int _sectionCount; // Because ConcurrentDictionary<,>.Count is locking.
        private int _maxSizeKb = DefaultMaxSizeKb;
        private int _maxFileSizeKb = DefaultMaxFileSizeKb;
        private PeriodicTask? _cleaner;

        /// <summary>
        /// Gets the default <see cref="FileCache"/> instance used by <see cref="FileModule"/>.
        /// </summary>
        public static FileCache Default
        {
            get
            {
                if (_defaultInstance != null)
                    return _defaultInstance;

                lock (DefaultSyncRoot)
                {
                    if (_defaultInstance == null)
                        _defaultInstance = new FileCache();
                }

                return _defaultInstance;
            }
        }

        /// <summary>
        /// <para>Gets or sets the maximum total size of cached data in kilobytes (1 kilobyte = 1024 bytes).</para>
        /// <para>The default value for this property is stored in the <see cref="DefaultMaxSizeKb"/> constant field.</para>
        /// <para>Setting this property to a value less lower han 1 has the same effect as setting it to 1.</para>
        /// </summary>
        public int MaxSizeKb
        {
            get => _maxSizeKb;
            set => _maxSizeKb = Math.Max(value, 1);
        }

        /// <summary>
        /// <para>Gets or sets the maximum size of a single cached file in kilobytes (1 kilobyte = 1024 bytes).</para>
        /// <para>A single file's contents may be present in a cache more than once, if the file
        /// is requested with different <c>Accept-Encoding</c> request headers. This property acts as a threshold
        /// for the uncompressed size of a file.</para>
        /// <para>The default value for this property is stored in the <see cref="DefaultMaxFileSizeKb"/> constant field.</para>
        /// <para>Setting this property to a value lower than 0 has the same effect as setting it to 0, in fact
        /// completely disabling the caching of file contents for this cache.</para>
        /// <para>This property cannot be set to a value higher than 2097151; in other words, it is not possible
        /// to cache files bigger than two Gigabytes (1 Gigabyte = 1048576 kilobytes) minus 1 kilobyte.</para>
        /// </summary>
        public int MaxFileSizeKb
        {
            get => _maxFileSizeKb;
            set => _maxFileSizeKb = Math.Min(Math.Max(value, 0), 2097151);
        }

        // Cast as IDictionary because we WANT an exception to be thrown if the name exists.
        // It would mean that something is very, very wrong.
        internal Section AddSection(string name)
        {
            var section = new Section();
            (_sections as IDictionary<string, Section>).Add(name, section);

            if (Interlocked.Increment(ref _sectionCount) == 1)
                _cleaner = new PeriodicTask(TimeSpan.FromMinutes(1), CheckMaxSize);

            return section;
        }

        internal void RemoveSection(string name)
        {
            _sections.TryRemove(name, out _);

            if (Interlocked.Decrement(ref _sectionCount) == 0)
            {
                _cleaner?.Dispose();
                _cleaner = null;
            }
        }

        private async Task CheckMaxSize(CancellationToken cancellationToken)
        {
            var timeKeeper = new TimeKeeper();
            var maxSizeKb = _maxSizeKb;
            var initialSizeKb = ComputeTotalSize() / 1024L;

            if (initialSizeKb <= maxSizeKb)
            {
                $"Total size = {initialSizeKb}/{_maxSizeKb}kb, not purging.".Debug(nameof(FileCache));
                return;
            }

            $"Total size = {initialSizeKb}/{_maxSizeKb}kb, purging...".Debug(nameof(FileCache));

            var removedCount = 0;
            var removedSize = 0L;
            var totalSizeKb = initialSizeKb;
            var threshold = 973L * maxSizeKb / 1024L; // About 95% of maximum allowed size
            while (totalSizeKb > threshold)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                var section = GetSectionWithLeastRecentItem();
                if (section == null)
                    return;

                removedSize += section.RemoveLeastRecentItem();
                removedCount++;

                await Task.Yield();

                totalSizeKb = ComputeTotalSize() / 1024L;
            }

            $"Purge completed in {timeKeeper.ElapsedTime}ms: removed {removedCount} items ({removedSize / 1024L}kb). Total size is now {totalSizeKb}kb."
                .Debug(nameof(FileCache));
        }

        // Enumerate key / value pairs because the Keys and Values property
        // of ConcurrentDictionary<,> have snapshot semantics,
        // while GetEnumerator enumerates without locking.
        private long ComputeTotalSize()
            => _sections.Sum(pair => pair.Value.GetTotalSize());

        private Section? GetSectionWithLeastRecentItem()
        {
            Section? result = null;
            var earliestTime = long.MaxValue;
            foreach (var pair in _sections)
            {
                var section = pair.Value;
                var time = section.GetLeastRecentUseTime();
               
                if (time < earliestTime)
                {
                    result = section;
                    earliestTime = time;
                }
            }

            return result;
        }
    }
}