using System;
using System.Collections.Generic;

namespace EmbedIO.Sessions.Internal
{
    internal sealed class DummySessionProxy : ISessionProxy
    {
        private DummySessionProxy()
        {
        }

        public static ISessionProxy Instance { get; } = new DummySessionProxy();

        public bool Exists => false;

        /// <inheritdoc/>
        public string Id => throw NoSessionManager();

        /// <inheritdoc/>
        public TimeSpan Duration => throw NoSessionManager();

        /// <inheritdoc/>
        public DateTime LastActivity => throw NoSessionManager();

        /// <inheritdoc/>
        public int Count => 0;

        /// <inheritdoc/>
        public bool IsEmpty => true;

        /// <inheritdoc/>
        public object this[string key]
        {
            get => throw NoSessionManager();
            set => throw NoSessionManager();
        }

        /// <inheritdoc/>
        public void Delete()
        {
        }

        /// <inheritdoc/>
        public void Regenerate() => throw NoSessionManager();

        /// <inheritdoc/>
        public void Clear()
        {
        }

        /// <inheritdoc/>
        public bool ContainsKey(string key) => throw NoSessionManager();

        /// <inheritdoc/>
        public bool TryGetValue(string key, out object value) => throw NoSessionManager();

        /// <inheritdoc/>
        public bool TryRemove(string key, out object value) => throw NoSessionManager();

        /// <inheritdoc/>
        public IReadOnlyList<KeyValuePair<string, object>> TakeSnapshot() => throw NoSessionManager();

        private InvalidOperationException NoSessionManager() => new InvalidOperationException("No session manager registered in the web server.");
    }
}