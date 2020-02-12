using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Utilities;

namespace EmbedIO.Sessions
{
    /// <summary>
    /// <para>A simple session manager to handle in-memory sessions.</para>
    /// <para>Not for intensive use or for distributed applications.</para>
    /// </summary>
    public partial class LocalSessionManager : ISessionManager
    {
        /// <summary>
        /// The default name for session cookies, i.e. <c>"__session"</c>.
        /// </summary>
        public const string DefaultCookieName = "__session";

        /// <summary>
        /// The default path for session cookies, i.e. <c>"/"</c>.
        /// </summary>
        public const string DefaultCookiePath = "/";

        /// <summary>
        /// The default HTTP-only flag for session cookies, i.e. <see langword="true"/>.
        /// </summary>
        public const bool DefaultCookieHttpOnly = true;

        /// <summary>
        /// The default duration for session cookies, i.e. <see cref="TimeSpan.Zero"/>.
        /// </summary>
        public static readonly TimeSpan DefaultCookieDuration = TimeSpan.Zero;

        /// <summary>
        /// The default duration for sessions, i.e. 30 minutes.
        /// </summary>
        public static readonly TimeSpan DefaultSessionDuration = TimeSpan.FromMinutes(30);

        /// <summary>
        /// The default interval between automatic purges of expired and empty sessions, i.e. 30 seconds.
        /// </summary>
        public static readonly TimeSpan DefaultPurgeInterval = TimeSpan.FromSeconds(30);

        private readonly ConcurrentDictionary<string, SessionImpl> _sessions =
            new ConcurrentDictionary<string, SessionImpl>(Session.KeyComparer);

        private string _cookieName = DefaultCookieName;

        private string _cookiePath = DefaultCookiePath;

        private TimeSpan _cookieDuration = DefaultCookieDuration;

        private bool _cookieHttpOnly = DefaultCookieHttpOnly;

        private TimeSpan _sessionDuration = DefaultSessionDuration;

        private TimeSpan _purgeInterval = DefaultPurgeInterval;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalSessionManager"/> class
        /// with default values for all properties.
        /// </summary>
        /// <seealso cref="DefaultSessionDuration"/>
        /// <seealso cref="DefaultPurgeInterval"/>
        /// <seealso cref="DefaultCookieName"/>
        /// <seealso cref="DefaultCookiePath"/>
        /// <seealso cref="DefaultCookieDuration"/>
        /// <seealso cref="DefaultCookieHttpOnly"/>
        public LocalSessionManager()
        {
        }

        /// <summary>
        /// Gets or sets the duration of newly-created sessions.
        /// </summary>
        /// <exception cref="InvalidOperationException">This property is being set after calling
        /// the <see cref="Start"/> method.</exception>
        /// <seealso cref="DefaultSessionDuration"/>
        public TimeSpan SessionDuration
        {
            get => _sessionDuration;
            set
            {
                EnsureConfigurationNotLocked();
                _sessionDuration = value;
            }
        }

        /// <summary>
        /// Gets or sets the interval between purges of expired sessions.
        /// </summary>
        /// <exception cref="InvalidOperationException">This property is being set after calling
        /// the <see cref="Start"/> method.</exception>
        /// <seealso cref="DefaultPurgeInterval"/>
        public TimeSpan PurgeInterval
        {
            get => _purgeInterval;
            set
            {
                EnsureConfigurationNotLocked();
                _purgeInterval = value;
            }
        }

        /// <summary>
        /// <para>Gets or sets the name for session cookies.</para>
        /// </summary>
        /// <exception cref="InvalidOperationException">This property is being set after calling
        /// the <see cref="Start"/> method.</exception>
        /// <exception cref="ArgumentNullException">This property is being set to <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">This property is being set and the provided value
        /// is not a valid URL path.</exception>
        /// <seealso cref="DefaultCookieName"/>
        public string CookieName
        {
            get => _cookieName;
            set
            {
                EnsureConfigurationNotLocked();
                _cookieName = Validate.Rfc2616Token(nameof(value), value);
            }
        }

        /// <summary>
        /// <para>Gets or sets the path for session cookies.</para>
        /// </summary>
        /// <exception cref="InvalidOperationException">This property is being set after calling
        /// the <see cref="Start"/> method.</exception>
        /// <exception cref="ArgumentNullException">This property is being set to <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">This property is being set and the provided value
        /// is not a valid URL path.</exception>
        /// <seealso cref="DefaultCookiePath"/>
        public string CookiePath
        {
            get => _cookiePath;
            set
            {
                EnsureConfigurationNotLocked();
                _cookiePath = Validate.UrlPath(nameof(value), value, true);
            }
        }

        /// <summary>
        /// Gets or sets the duration of session cookies.
        /// </summary>
        /// <exception cref="InvalidOperationException">This property is being set after calling
        /// the <see cref="Start"/> method.</exception>
        /// <seealso cref="DefaultCookieDuration"/>
        public TimeSpan CookieDuration
        {
            get => _cookieDuration;
            set
            {
                EnsureConfigurationNotLocked();
                _cookieDuration = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether session cookies are hidden from Javascript code running on a user agent.
        /// </summary>
        /// <exception cref="InvalidOperationException">This property is being set after calling
        /// the <see cref="Start"/> method.</exception>
        /// <seealso cref="DefaultCookieHttpOnly"/>
        public bool CookieHttpOnly
        {
            get => _cookieHttpOnly;
            set
            {
                EnsureConfigurationNotLocked();
                _cookieHttpOnly = value;
            }
        }

        private bool ConfigurationLocked { get; set; }

        /// <inheritdoc />
        public void Start(CancellationToken cancellationToken)
        {
            ConfigurationLocked = true;

            Task.Run(async () =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        PurgeExpiredAndEmptySessions();
                        await Task.Delay(PurgeInterval, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (TaskCanceledException)
                {
                    // ignore
                }
            }, cancellationToken);
        }

        /// <inheritdoc />
        public ISession Create(IHttpContext context)
        {
            var id = context.Request.Cookies.FirstOrDefault(IsSessionCookie)?.Value.Trim();

            SessionImpl session;
            lock (_sessions)
            {
                if (!string.IsNullOrEmpty(id) && _sessions.TryGetValue(id!, out session))
                {
                    session.BeginUse();
                }
                else
                {
                    id = UniqueIdGenerator.GetNext();
                    session = new SessionImpl(id, SessionDuration);
                    _sessions.TryAdd(id, session);
                }
            }

            context.Request.Cookies.Add(BuildSessionCookie(id));
            context.Response.Cookies.Add(BuildSessionCookie(id));
            return session;
        }

        /// <inheritdoc />
        public void Delete(IHttpContext context, string id)
        {
            lock (_sessions)
            {
                if (_sessions.TryGetValue(id, out var session))
                    session.EndUse(() => _sessions.TryRemove(id, out _));
            }

            context.Request.Cookies.Add(BuildSessionCookie(string.Empty));
            context.Response.Cookies.Add(BuildSessionCookie(string.Empty));
        }

        /// <inheritdoc />
        public void OnContextClose(IHttpContext context)
        {
            if (!context.Session.Exists)
                return;

            var id = context.Session.Id;
            lock (_sessions)
            {
                if (_sessions.TryGetValue(id, out var session))
                {
                    session.EndUse(() => _sessions.TryRemove(id, out _));
                }
            }
        }

        private void EnsureConfigurationNotLocked()
        {
            if (ConfigurationLocked)
                throw new InvalidOperationException($"Cannot configure a {nameof(LocalSessionManager)} once it has been started.");
        }

        private bool IsSessionCookie(Cookie cookie)
            => cookie.Name.Equals(CookieName, StringComparison.OrdinalIgnoreCase)
             && !cookie.Expired;

        private Cookie BuildSessionCookie(string? id)
        {
            var cookie = new Cookie(CookieName, id, CookiePath)
            {
                HttpOnly = CookieHttpOnly,
            };

            if (CookieDuration > TimeSpan.Zero)
            {
                cookie.Expires = DateTime.UtcNow.Add(CookieDuration);
            }

            return cookie;
        }

        private void PurgeExpiredAndEmptySessions()
        {
            string[] ids;
            lock (_sessions)
            {
                ids = _sessions.Keys.ToArray();
            }

            foreach (var id in ids)
            {
                lock (_sessions)
                {
                    if (!_sessions.TryGetValue(id, out var session))
                        return;

                    session.UnregisterIfNeeded(() => _sessions.TryRemove(id, out _));
                }
            }
        }

        private string GetSessionId(IHttpContext context) => context.Request.Cookies.FirstOrDefault(IsSessionCookie)?.Value.Trim() ?? string.Empty;
    }
}
