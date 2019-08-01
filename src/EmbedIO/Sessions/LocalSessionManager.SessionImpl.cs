using System;
using EmbedIO.Utilities;
using Swan.Components;

namespace EmbedIO.Sessions
{
    partial class LocalSessionManager
    {
        private class SessionImpl : ISession
        {
            private readonly DataDictionary<string, object> _data = new DataDictionary<string, object>(Session.KeyComparer);

            private int _usageCount;

            public SessionImpl(string id, TimeSpan duration)
            {
                Id = Validate.NotNullOrEmpty(nameof(id), id);
                Duration = duration;
                LastActivity = DateTime.UtcNow;
                _usageCount = 1;
            }

            public string Id { get; }

            public TimeSpan Duration { get; }

            public DateTime LastActivity { get; private set; }

            public int Count
            {
                get
                {
                    lock (_data)
                    {
                        return _data.Count;
                    }
                }
            }

            public bool IsEmpty
            {
                get
                {
                    lock (_data)
                    {
                        return _data.IsEmpty;
                    }
                }
            }

            public object this[string key]
            {
                get
                {
                    lock (_data)
                    {
                        return _data[key];
                    }
                }
                set
                {
                    lock (_data)
                    {
                        _data[key] = value;
                    }
                }
            }

            public void Clear()
            {
                lock (_data)
                {
                    _data.Clear();
                }
            }

            public bool ContainsKey(string key)
            {
                lock (_data)
                {
                    return _data.ContainsKey(key);
                }
            }

            public bool TryRemove(string key, out object value)
            {
                lock (_data)
                {
                    return _data.TryRemove(key, out value);
                }
            }

            public bool TryGetValue(string key, out object value)
            {
                lock (_data)
                {
                    return _data.TryGetValue(key, out value);
                }
            }

            internal void BeginUse()
            {
                lock (_data)
                {
                    _usageCount++;
                    LastActivity = DateTime.UtcNow;
                }
            }

            internal void EndUse(Action unregister)
            {
                lock (_data)
                {
                    --_usageCount;
                    UnregisterIfNeededCore(unregister);
                }
            }

            internal void UnregisterIfNeeded(Action unregister)
            {
                lock (_data)
                {
                    UnregisterIfNeededCore(unregister);
                }
            }

            private void UnregisterIfNeededCore(Action unregister)
            {
                if (_usageCount < 1 && (IsEmpty || DateTime.UtcNow > LastActivity + Duration))
                    unregister();
            }
        }
    }
}