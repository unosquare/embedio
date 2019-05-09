using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Constants;
using EmbedIO.Utilities;
using Unosquare.Swan;

namespace EmbedIO.Modules
{
    /// <summary>
    /// A simple session manager to handle in-memory sessions. Do not use for distributed applications.
    /// </summary>
    public class LocalSessionManager : ISessionManager
    {
        /// <summary>
        /// Defines the session cookie name.
        /// </summary>
        private const string SessionCookieName = "__session";

        /// <summary>
        /// The concurrent dictionary holding the sessions.
        /// </summary>
        private readonly ConcurrentDictionary<string, SessionInfo> _sessions =
            new ConcurrentDictionary<string, SessionInfo>(StringComparer.InvariantCulture);

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalSessionManager"/> class.
        /// </summary>
        public LocalSessionManager()
        {
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, SessionInfo> Sessions => new Dictionary<string, SessionInfo>(_sessions);

        /// <inheritdoc />
        /// <remarks>
        /// <para>By default, expiration for <see cref="LocalSessionManager"/> sessions is 30 minutes.</para>
        /// </remarks>
        public TimeSpan Expiration { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Gets or sets the interval between purges of expired sessions.
        /// </summary>
        /// <remarks>
        /// <para>By default, the purge interval for <see cref="LocalSessionManager"/> is 30 seconds.</para>
        /// </remarks>
        public TimeSpan PurgeInterval { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the cookie path.
        /// If left empty, a cookie will be created for each path. The default value is "/"
        /// If a route is specified, then session cookies will be created only for the given path.
        /// Examples of this are:
        ///     "/"
        ///     "/app1/".
        /// </summary>
        /// <value>
        /// The cookie path.
        /// </value>
        public string CookiePath { get; set; } = "/";

        /// <summary>
        /// Gets the <see cref="SessionInfo"/> with the specified cookie value.
        /// Returns null when the session is not found.
        /// </summary>
        /// <value>
        /// The <see cref="SessionInfo"/>.
        /// </value>
        /// <param name="cookieValue">The cookie value.</param>
        /// <returns>Session info with the specified cookie value.</returns>
        public SessionInfo this[string cookieValue] => _sessions.TryGetValue(cookieValue, out var value) ? value : null;

        /// <inheritdoc />
        public SessionInfo GetSession(IHttpContext context)
        {
            if (context.Request.Cookies[SessionCookieName] == null) return null;

            var cookieValue = context.Request.Cookies[SessionCookieName].Value;
            return this[cookieValue];
        }

        /// <inheritdoc />
        public SessionInfo GetSession(IWebSocketContext context)
        {
            if (context.CookieCollection[SessionCookieName] == null) return null;

            var cookieValue = context.CookieCollection[SessionCookieName].Value;
            return this[cookieValue];
        }

        /// <inheritdoc />
        public void DeleteSession(IHttpContext context) => DeleteSession(GetSession(context));

        /// <inheritdoc />
        public void DeleteSession(SessionInfo session) => _sessions.TryRemove(session.Id, out _);

        /// <inheritdoc />
        public void OnStart(CancellationToken ct)
        {
            Task.Run(async () =>
            {
                try
                {
                    while (!ct.IsCancellationRequested)
                    {
                        PurgeExpiredSessions();
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
        public void OnRequest(IHttpContext context, CancellationToken ct)
        {
            var requestSessionCookie = context.Request.Cookies[SessionCookieName];
            var isSessionRegistered = false;

            if (requestSessionCookie != null)
            {
                FixUpSessionCookie(context);
                isSessionRegistered = _sessions.ContainsKey(requestSessionCookie.Value);
            }

            if (requestSessionCookie == null)
            {
                // create the session if session not available on the request
                var sessionCookie = CreateSession();
                context.Response.SetCookie(sessionCookie);
                context.Request.Cookies.Add(sessionCookie);
                $"Created session identifier '{sessionCookie.Value}'".Debug(nameof(LocalSessionManager));
            }
            else if (!isSessionRegistered)
            {
                // update session value
                var sessionCookie = CreateSession();
                context.Response.SetCookie(sessionCookie); // = sessionCookie.Value;
                context.Request.Cookies[SessionCookieName].Value = sessionCookie.Value;
                $"Updated session identifier to '{sessionCookie.Value}'".Debug(nameof(LocalSessionManager));
            }
            else
            {
                // If it does exist in the request, check if we're tracking it
                var requestSessionId = context.Request.Cookies[SessionCookieName].Value;
                _sessions[requestSessionId].LastActivity = DateTime.UtcNow;
                $"Session Identified '{requestSessionId}'".Debug(nameof(LocalSessionManager));
            }
        }

        /// <summary>
        /// Creates a session ID, registers the session info in the Sessions collection, and returns the appropriate session cookie.
        /// </summary>
        /// <returns>The sessions.</returns>
        private System.Net.Cookie CreateSession()
        {
            var sessionId = UniqueIdGenerator.GetNext();

            var sessionCookie = string.IsNullOrWhiteSpace(CookiePath)
                ? new System.Net.Cookie(SessionCookieName, sessionId)
                : new System.Net.Cookie(SessionCookieName, sessionId, CookiePath);

            _sessions[sessionId] = new SessionInfo(sessionId);

            return sessionCookie;
        }

        /// <summary>
        /// Fixes the session cookie to match the correct value.
        /// System.Net.Cookie.Value only supports a single value and we need to pick the one that potentially exists.
        /// </summary>
        /// <param name="context">The context.</param>
        private void FixUpSessionCookie(IHttpContext context)
        {
            // get the real "__session" cookie value because sometimes there's more than 1 value and System.Net.Cookie only supports 1 value per cookie
            if (context.Request.Headers[HttpHeaders.Cookie] == null) return;

            var cookieItems = context.Request.Headers[HttpHeaders.Cookie]
                .SplitByAny(StringSplitOptions.RemoveEmptyEntries, ',', ';');

            foreach (var cookieItem in cookieItems)
            {
                var nameValue = cookieItem.Trim().Split(new[] {'='}, StringSplitOptions.RemoveEmptyEntries);

                if (nameValue.Length != 2 || !nameValue[0].Equals(SessionCookieName)) continue;

                var sessionIdValue = nameValue[1].Trim();

                if (!_sessions.ContainsKey(sessionIdValue)) continue;

                context.Request.Cookies[SessionCookieName].Value = sessionIdValue;
                break;
            }
        }

        private void PurgeExpiredSessions()
        {
            var now = DateTime.UtcNow;
            _sessions.Values
                .Where(x => x.LastActivity.Add(Expiration) > now)
                .ToList()
                .ForEach(DeleteSession);
        }
    }
}