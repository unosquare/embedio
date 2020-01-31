using System;
using System.Collections.Generic;
using EmbedIO.Files.Internal;

namespace EmbedIO.Files
{
    public sealed partial class FileCache
    {
        internal class Section
        {
            private readonly object _syncRoot = new object();
            private readonly Dictionary<string, FileCacheItem> _items = new Dictionary<string, FileCacheItem>(StringComparer.Ordinal);
            private long _totalSize;
            private string? _oldestKey;
            private string? _newestKey;

            public void Clear()
            {
                lock (_syncRoot)
                {
                    ClearCore();
                }
            }

            public void Add(string path, FileCacheItem item)
            {
                lock (_syncRoot)
                {
                    AddItemCore(path, item);
                }
            }

            public void Remove(string path)
            {
                lock (_syncRoot)
                {
                    RemoveItemCore(path);
                }
            }

            public bool TryGet(string path, out FileCacheItem item)
            {
                lock (_syncRoot)
                {
                    if (!_items.TryGetValue(path, out item))
                        return false;

                    RefreshItemCore(path, item);
                    return true;
                }
            }

            internal long GetLeastRecentUseTime()
            {
                lock (_syncRoot)
                {
                    return _oldestKey == null ? long.MaxValue : _items[_oldestKey].LastUsedAt;
                }
            }

            // Removes least recently used item.
            // Returns size of removed item.
            internal long RemoveLeastRecentItem()
            {
                lock (_syncRoot)
                {
                    return RemoveLeastRecentItemCore();
                }
            }

            internal long GetTotalSize()
            {
                lock (_syncRoot)
                {
                    return _totalSize;
                }
            }

            internal void UpdateTotalSize(long delta)
            {
                lock (_syncRoot)
                {
                    _totalSize += delta;
                }
            }

            private void ClearCore()
            {
                _items.Clear();
                _totalSize = 0;
                _oldestKey = null;
                _newestKey = null;
            }

            // Adds an item as most recently used.
            private void AddItemCore(string path, FileCacheItem item)
            {
                item.PreviousKey = _newestKey;
                item.NextKey = null;
                item.LastUsedAt = TimeBase.ElapsedTicks;

                if (_newestKey != null)
                    _items[_newestKey].NextKey = path;

                _newestKey = path;

                _items[path] = item;
                _totalSize += item.SizeInCache;
            }

            // Removes an item.
            private void RemoveItemCore(string path)
            {
                if (!_items.TryGetValue(path, out var item))
                    return;

                if (_oldestKey == path)
                    _oldestKey = item.NextKey;

                if (_newestKey == path)
                    _newestKey = item.PreviousKey;

                if (item.PreviousKey != null)
                    _items[item.PreviousKey].NextKey = item.NextKey;

                if (item.NextKey != null)
                    _items[item.NextKey].PreviousKey = item.PreviousKey;

                item.PreviousKey = null;
                item.NextKey = null;

                _items.Remove(path);
                _totalSize -= item.SizeInCache;
            }

            // Removes the least recently used item.
            // returns size of removed item.
            private long RemoveLeastRecentItemCore()
            {
                var path = _oldestKey;
                if (path == null)
                    return 0;

                var item = _items[path];

                if ((_oldestKey = item.NextKey) != null)
                    _items[_oldestKey].PreviousKey = null;

                if (_newestKey == path)
                    _newestKey = null;

                item.PreviousKey = null;
                item.NextKey = null;

                _items.Remove(path);
                _totalSize -= item.SizeInCache;
                return item.SizeInCache;
            }

            // Moves an item to most recently used.
            private void RefreshItemCore(string path, FileCacheItem item)
            {
                item.LastUsedAt = TimeBase.ElapsedTicks;

                if (_newestKey == path)
                    return;

                if (_oldestKey == path)
                    _oldestKey = item.NextKey;

                if (item.PreviousKey != null)
                    _items[item.PreviousKey].NextKey = item.NextKey;

                if (item.NextKey != null)
                    _items[item.NextKey].PreviousKey = item.PreviousKey;

                item.PreviousKey = _newestKey;
                item.NextKey = null;

                _items[_newestKey!].NextKey = path;
                _newestKey = path;
            }
        }
    }
}