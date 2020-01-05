using System;
using System.Collections.Generic;
using EmbedIO.Sessions.Internal;

namespace EmbedIO.Sessions
{
    /// <summary>
    /// Provides the same interface as a session object,
    /// plus a basic interface to a session manager.
    /// </summary>
    /// <remarks>
    /// A session proxy can be used just as if it were a session object.
    /// A session is automatically created wherever its data are accessed.
    /// </remarks>
    /// <seealso cref="ISessionProxy" />
    public sealed class SessionProxy : ISessionProxy
    {
        private readonly IHttpContext _context;
        private readonly ISessionManager? _sessionManager;

        private ISession? _session;
        private bool _onCloseRegistered;

        internal SessionProxy(IHttpContext context, ISessionManager? sessionManager)
        {
            _context = context;
            _sessionManager = sessionManager;
        }

        /// <summary>
        /// Returns a "dummy" <see cref="ISessionProxy"/> interface that will always behave as if no session manager has been defined.
        /// </summary>
        /// <remarks>
        /// <para>The returned <see cref="ISessionProxy"/> interface is only useful
        /// to initialize a non-nullable property of type <see cref="ISessionProxy"/>.</para>
        /// </remarks>
        public static ISessionProxy None => DummySessionProxy.Instance;

        /// <inheritdoc/>
        public bool Exists => _session != null;

        /// <inheritdoc/>
        public string Id
        {
            get
            {
                EnsureSessionExists();
                return _session!.Id;
            }
        }

        /// <inheritdoc/>
        public TimeSpan Duration
        {
            get
            {
                EnsureSessionExists();
                return _session!.Duration;
            }
        }

        /// <inheritdoc/>
        public DateTime LastActivity
        {
            get
            {
                EnsureSessionExists();
                return _session!.LastActivity;
            }
        }

        /// <inheritdoc/>
        public int Count => _session?.Count ?? 0;

        /// <inheritdoc/>
        public bool IsEmpty => _session?.IsEmpty ?? true;

        /// <inheritdoc/>
        public object this[string key]
        {
            get
            {
                EnsureSessionExists();
                return _session![key];
            }
            set
            {
                EnsureSessionExists();
                _session![key] = value;
            }
        }

        /// <inheritdoc/>
        public void Delete()
        {
            EnsureSessionExists();

            if (_session == null)
                return;

            _sessionManager!.Delete(_context, _session.Id);
            _session = null;
        }

        /// <inheritdoc/>
        public void Regenerate()
        {
            if (_session != null)
                _sessionManager!.Delete(_context, _session.Id);

            EnsureSessionManagerExists();
            _session = _sessionManager!.Create(_context);
        }

        /// <inheritdoc/>
        public void Clear() => _session?.Clear();

        /// <inheritdoc/>
        public bool ContainsKey(string key)
        {
            EnsureSessionExists();
            return _session!.ContainsKey(key);
        }

        /// <inheritdoc/>
        public bool TryGetValue(string key, out object value)
        {
            EnsureSessionExists();
            return _session!.TryGetValue(key, out value);
        }

        /// <inheritdoc/>
        public bool TryRemove(string key, out object value)
        {
            EnsureSessionExists();
            return _session!.TryRemove(key, out value);
        }

        /// <inheritdoc/>
        public IReadOnlyList<KeyValuePair<string, object>> TakeSnapshot()
        {
            EnsureSessionExists();
            return _session!.TakeSnapshot();
        }

        private void EnsureSessionManagerExists()
        {
            if (_sessionManager == null)
                throw new InvalidOperationException("No session manager registered in the web server.");
        }

        private void EnsureSessionExists()
        {
            if (_session != null)
                return;

            EnsureSessionManagerExists();
            _session = _sessionManager!.Create(_context);

            if (_onCloseRegistered)
                return;

            _context.OnClose(_sessionManager.OnContextClose);
            _onCloseRegistered = true;
        }
    }
}