using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Constants;
using EmbedIO.Utilities;

namespace EmbedIO
{
    /// <summary>
    /// <para>A simple session manager to handle in-memory sessions.</para>
    /// <para>Not for intensive use or for distributed applications.</para>
    /// </summary>
    public partial class LocalSessionManager : ISessionManager
    {
        private readonly ConcurrentDictionary<string, SessionImpl> _sessions =
            new ConcurrentDictionary<string, SessionImpl>(Session.KeyComparer);

        private string _cookieName;
        private string _cookiePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalSessionManager"/> class.
        /// </summary>
        /// <param name="cookieName">The name of the session cookie.</param>
        /// <param name="cookiePath">The path of the session cookie.</param>
        /// <param name="cookieDuration">The duration of the session cookie.</param>
        /// <param name="cookieHttpOnly"><see langword="true"/> to hide the session cookie from Javascript running on a user agent.</param>
        /// <seealso cref="CookieName"/>
        /// <seealso cref="CookiePath"/>
        /// <seealso cref="CookieDuration"/>
        /// <seealso cref="CookieHttpOnly"/>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="cookieName"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="cookiePath"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="cookieName"/> is empty or contains one or more invalid characters.</para>
        /// <para>- or -</para>
        /// <para><paramref name="cookiePath"/> is not a valid base URL path.</para>
        /// </exception>
        public LocalSessionManager(string cookieName, string cookiePath, TimeSpan cookieDuration, bool cookieHttpOnly = true)
        {
            _cookieName = Validate.CookieName(nameof(cookieName), cookieName);
            _cookiePath = Validate.UrlPath(nameof(cookiePath), cookiePath, true);
            CookieDuration = cookieDuration;
            CookieHttpOnly = cookieHttpOnly;
        }

        /// <summary>
        /// Gets or sets the duration of newly-created sessions.
        /// </summary>
        /// <value>
        /// The duration of a session.
        /// </value>
        /// <remarks>
        /// By default, the duration for <see cref="LocalSessionManager"/> sessions is 30 minutes.
        /// </remarks>
        public TimeSpan SessionDuration { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Gets or sets the interval between purges of expired sessions.
        /// </summary>
        /// <remarks>
        /// <para>By default, the purge interval for <see cref="LocalSessionManager"/> is 30 seconds.</para>
        /// </remarks>
        public TimeSpan PurgeInterval { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// <para>Gets or sets the name for session cookies. The default value is <c>"__session"</c>.</para>
        /// </summary>
        /// <value>
        /// The cookie path.
        /// </value>
        /// <exception cref="ArgumentNullException">This property is being set to <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">This property is being set and the provided value
        /// is not a valid URL path.</exception>
        public string CookieName
        {
            get => _cookieName;
            set => _cookieName = Validate.CookieName(nameof(value), value);
        }

        /// <summary>
        /// <para>Gets or sets the path for session cookies. The default value is <c>"/"</c>.</para>
        /// </summary>
        /// <value>
        /// The cookie path.
        /// </value>
        /// <exception cref="ArgumentNullException">This property is being set to <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">This property is being set and the provided value
        /// is not a valid URL path.</exception>
        public string CookiePath
        {
            get => _cookiePath;
            set => _cookiePath = Validate.UrlPath(nameof(value), value, true);
        }

        /// <summary>
        /// Gets or sets a value indicating whether session cookies are hidden from Javascript code running on a user agent.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if session cookies are flagged as HTTP-only; otherwise, <see langword="false"/>.
        /// </value>
        public bool CookieHttpOnly { get; set; }

        /// <summary>
        /// Gets or sets the duration of the session cookie.
        /// </summary>
        /// <value>
        /// The duration of the session cookie.
        /// </value>
        public TimeSpan CookieDuration { get; set; }

        /// <inheritdoc />
        public void Start(CancellationToken ct)
        {
            Task.Run(async () =>
            {
                try
                {
                    while (!ct.IsCancellationRequested)
                    {
                        PurgeExpiredAndEmptySessions();
                        await Task.Delay(PurgeInterval, ct).ConfigureAwait(false);
                    }
                }
                catch (TaskCanceledException)
                {
                    // ignore
                }
            }, ct);
        }

        /// <inheritdoc />
        public ISession Create(IHttpContext context)
        {
            var id = context.Request.Cookies.FirstOrDefault(IsSessionCookie)?.Value.Trim();

            SessionImpl session;
            lock (_sessions)
            {
                if (!string.IsNullOrEmpty(id) && _sessions.TryGetValue(id, out session))
                {
                    session.BeginUse();
                }
                else
                {
                    id = UniqueIdGenerator.GetNext();
                    session = new SessionImpl(id, SessionDuration);
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
                {
                    session.EndUse(() => _sessions.TryRemove(id, out _));
                }
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

        private bool IsSessionCookie(Cookie cookie)
            => cookie.Name.Equals(CookieName, StringComparison.OrdinalIgnoreCase)
             && cookie.Path.Equals(CookiePath, StringComparison.Ordinal)
             && string.IsNullOrEmpty(cookie.Domain)
             && !cookie.Expired;

        private Cookie BuildSessionCookie(string id)
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
            _sessions.Keys.ToList().ForEach(id => 
            {
                lock (_sessions)
                {
                    if (!_sessions.TryGetValue(id, out var session))
                        return;

                    session.UnregisterIfNeeded(() => _sessions.TryRemove(id, out _));
                }
            });
        }
    }
}