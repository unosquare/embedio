namespace Unosquare.Labs.EmbedIO.Modules
{
    using Swan;
    using System;
    using System.Collections.Concurrent;
    using System.IO;

    internal class RamCache : ConcurrentDictionary<string, RamCache.RamCacheEntry>
    {
        internal void Add(Stream buffer, string localPath, DateTime fileDate)
        {
            using (var memoryStream = new MemoryStream())
            {
                buffer.Position = 0;
                buffer.CopyTo(memoryStream);

                this[localPath] = new RamCacheEntry
                {
                    LastModified = fileDate,
                    Buffer = memoryStream.ToArray()
                };
            }
        }
        internal bool IsValid(string requestFullLocalPath, DateTime fileDate, out string currentHash)
        {
            if (ContainsKey(requestFullLocalPath) && this[requestFullLocalPath].LastModified == fileDate)
            {
                currentHash = this[requestFullLocalPath].Buffer.ComputeMD5().ToUpperHex() + '-' +
                              fileDate.Ticks;

                return true;
            }

            currentHash = string.Empty;
            return false;
        }

        /// <summary>
        /// Represents a RAM Cache dictionary entry
        /// </summary>
        internal class RamCacheEntry
        {
            public DateTime LastModified { get; set; }
            public byte[] Buffer { get; set; }
        }
    }
}