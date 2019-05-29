using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Unosquare.Swan;

namespace EmbedIO.Files.Internal
{
    internal class RamCache
    {
        readonly Lazy<ConcurrentDictionary<string, RamCacheEntry>> _data =
            new Lazy<ConcurrentDictionary<string, RamCacheEntry>>(() =>
                new ConcurrentDictionary<string, RamCacheEntry>());

        internal void Add(Stream buffer, string localPath, DateTime fileDate)
        {
            using (var memoryStream = new MemoryStream())
            {
                buffer.Position = 0;
                buffer.CopyTo(memoryStream);

                _data.Value[localPath] = new RamCacheEntry
                {
                    LastModified = fileDate,
                    Buffer = memoryStream.ToArray(),
                };
            }
        }

        internal bool IsValid(string requestFullLocalPath, DateTime fileDate, out string currentHash)
        {
            if (_data.Value.TryGetValue(requestFullLocalPath, out var item) && item.LastModified == fileDate)
            {
                currentHash = item.Buffer.ComputeMD5().ToUpperHex() + '-' +
                              fileDate.Ticks;

                return true;
            }

            currentHash = string.Empty;
            return false;
        }

        internal void Clear()
        {
            if (_data.IsValueCreated)
                _data.Value.Clear();
        }

        internal byte[] GetBuffer(string localPath) 
            => _data.Value.TryGetValue(localPath, out var item) ? item.Buffer : throw new KeyNotFoundException("The local path is not found");

        /// <summary>
        /// Represents a RAM Cache dictionary entry.
        /// </summary>
        internal class RamCacheEntry
        {
            public DateTime LastModified { get; set; }
            public byte[] Buffer { get; set; }
        }
    }
}