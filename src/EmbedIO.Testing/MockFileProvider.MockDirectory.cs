using System.Collections;
using System.Collections.Generic;

namespace EmbedIO.Testing
{
    partial class MockFileProvider
    {
        private sealed class MockDirectory : MockDirectoryEntry, IDictionary<string, MockDirectoryEntry>
        {
            readonly Dictionary<string, MockDirectoryEntry> _entries = new Dictionary<string, MockDirectoryEntry>();

            public IEnumerator<KeyValuePair<string, MockDirectoryEntry>> GetEnumerator() => _entries.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) _entries).GetEnumerator();

            public void Add(KeyValuePair<string, MockDirectoryEntry> item) => (_entries as ICollection<KeyValuePair<string, MockDirectoryEntry>>).Add(item);

            public void Clear() => _entries.Clear();

            public bool Contains(KeyValuePair<string, MockDirectoryEntry> item) => (_entries as ICollection<KeyValuePair<string, MockDirectoryEntry>>).Contains(item);

            public void CopyTo(KeyValuePair<string, MockDirectoryEntry>[] array, int arrayIndex) => (_entries as ICollection<KeyValuePair<string, MockDirectoryEntry>>).CopyTo(array, arrayIndex);

            public bool Remove(KeyValuePair<string, MockDirectoryEntry> item) => (_entries as ICollection<KeyValuePair<string, MockDirectoryEntry>>).Remove(item);

            public int Count => _entries.Count;

            public bool IsReadOnly => false;

            public void Add(string key, MockDirectoryEntry value) => _entries.Add(key, value);

            public void Add(string key, byte[] data) => _entries.Add(key, new MockFile(data));

            public void Add(string key, string data) => _entries.Add(key, new MockFile(data));

            public bool ContainsKey(string key) => _entries.ContainsKey(key);

            public bool Remove(string key) => _entries.Remove(key);

            public bool TryGetValue(string key, out MockDirectoryEntry value) => _entries.TryGetValue(key, out value);

            public MockDirectoryEntry this[string key]
            {
                get => _entries[key];
                set => _entries[key] = value;
            }

            public ICollection<string> Keys => _entries.Keys;

            public ICollection<MockDirectoryEntry> Values => _entries.Values;
        }
    }
}