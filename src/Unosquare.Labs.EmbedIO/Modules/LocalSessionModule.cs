namespace Unosquare.Labs.EmbedIO.Modules
{
    using EmbedIO;
    using System;
    using System.Threading.Tasks;
    using Swan;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
#if NET46
    using System.Net;
#else
    using Net;
#endif

    /// <summary>
    /// A simple module to handle in-memory sessions. Do not use for distributed applications
    /// </summary>
    public class LocalSessionModule : WebModuleBase, ISessionWebModule
    {
        /// <summary>
        /// Defines the session cookie name
        /// </summary>
        private const string SessionCookieName = "__session";

        /// <summary>
        /// The concurrent dictionary holding the sessions
        /// </summary>
        protected readonly Dictionary<string, SessionInfo> m_Sessions =
            new Dictionary<string, SessionInfo>(Constants.StandardStringComparer);

        /// <summary>
        /// The sessions dictionary synchronization lock
        /// </summary>
        protected readonly object SessionsSyncLock = new object();

        /// <summary>
        /// Creates a session ID, registers the session info in the Sessions collection, and returns the appropriate session cookie.
        /// </summary>
        /// <returns>The sessions.</returns>
        private System.Net.Cookie CreateSession()
        {
            lock (SessionsSyncLock)
            {
                var sessionId = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(
                        Guid.NewGuid() + DateTime.Now.Millisecond.ToString() + DateTime.Now.Ticks.ToString()));
                var sessionCookie = string.IsNullOrWhiteSpace(CookiePath) ?
                new System.Net.Cookie(SessionCookieName, sessionId) :
                new System.Net.Cookie(SessionCookieName, sessionId, CookiePath);

                m_Sessions[sessionId] = new SessionInfo()
                {
                    SessionId = sessionId,
                    DateCreated = DateTime.Now,
                    LastActivity = DateTime.Now
                };

                return sessionCookie;
            }
        }

        /// <summary>
        /// Delete the session object for the given context
        /// </summary>
        public void DeleteSession(HttpListenerContext context)
        {
            DeleteSession(GetSession(context));
        }

        /// <summary>
        /// Delete a session for the given session info
        /// </summary>
        /// <param name="session">The session info.</param>
        public void DeleteSession(SessionInfo session)
        {
            lock (SessionsSyncLock)
            {
                if (string.IsNullOrWhiteSpace(session?.SessionId)) return;
                if (m_Sessions.ContainsKey(session.SessionId) == false) return;
                m_Sessions.Remove(session.SessionId);
            }
        }

        /// <summary>
        /// Fixes the session cookie to match the correct value.
        /// System.Net.Cookie.Value only supports a single value and we need to pick the one that potentially exists.
        /// </summary>
        /// <param name="context">The context.</param>
        private void FixupSessionCookie(HttpListenerContext context)
        {
            // get the real "__session" cookie value because sometimes there's more than 1 value and System.Net.Cookie only supports 1 value per cookie
            if (context.Request.Headers[Constants.CookieHeader] == null) return;

            var cookieItems = context.Request.Headers[Constants.CookieHeader].Split(new[] { ';', ',' },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (var cookieItem in cookieItems)
            {
                var nameValue = cookieItem.Trim().Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (nameValue.Length == 2 && nameValue[0].Equals(SessionCookieName))
                {
                    var sessionIdValue = nameValue[1].Trim();

                    if (m_Sessions.ContainsKey(sessionIdValue))
                    {
                        context.Request.Cookies[SessionCookieName].Value = sessionIdValue;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalSessionModule"/> class.
        /// </summary>
        public LocalSessionModule()
        {
            AddHandler(ModuleMap.AnyPath, HttpVerbs.Any, (context, ct) =>
            {
                lock (SessionsSyncLock)
                {
                    var currentSessions = new Dictionary<string, SessionInfo>(m_Sessions);

                    // expire old sessions
                    foreach (var session in currentSessions)
                    {
                        if (session.Value == null) continue;

                        if (DateTime.Now.Subtract(session.Value.LastActivity) > Expiration)
                            DeleteSession(session.Value);
                    }

                    var requestSessionCookie = context.Request.Cookies[SessionCookieName];
                    var isSessionRegistered = false;

                    if (requestSessionCookie != null)
                    {
                        FixupSessionCookie(context);
                        isSessionRegistered = m_Sessions.ContainsKey(requestSessionCookie.Value);
                    }
                    
                    if (requestSessionCookie == null)
                    {
                        // create the session if session not available on the request
                        var sessionCookie = CreateSession();
                        context.Response.SetCookie(sessionCookie);
                        context.Request.Cookies.Add(sessionCookie);
                        $"Created session identifier '{sessionCookie.Value}'".Debug(nameof(LocalSessionModule));
                    }
                    else if (isSessionRegistered == false)
                    {
                        //update session value
                        var sessionCookie = CreateSession();
                        context.Response.SetCookie(sessionCookie); // = sessionCookie.Value;
                        context.Request.Cookies[SessionCookieName].Value = sessionCookie.Value;
                        $"Updated session identifier to '{sessionCookie.Value}'".Debug(nameof(LocalSessionModule));
                    }
                    else
                    {
                        // If it does exist in the request, check if we're tracking it
                        var requestSessionId = context.Request.Cookies[SessionCookieName].Value;
                        m_Sessions[requestSessionId].LastActivity = DateTime.Now;
                        $"Session Identified '{requestSessionId}'".Debug(nameof(LocalSessionModule));
                    }

                    // Always returns false because we need it to handle the rest for the modules
                    return Task.FromResult(false);
                }
            });
        }
        
        /// <summary>
        /// The dictionary holding the sessions
        /// </summary>
        /// <value>
        /// The sessions.
        /// </value>
        public IReadOnlyDictionary<string, SessionInfo> Sessions
        {
            get
            {
                lock (SessionsSyncLock)
                {
                    return new ReadOnlyDictionary<string, SessionInfo>(m_Sessions);
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="SessionInfo"/> with the specified cookie value.
        /// Returns null when the session is not found.
        /// </summary>
        /// <value>
        /// The <see cref="SessionInfo"/>.
        /// </value>
        /// <param name="cookieValue">The cookie value.</param>
        /// <returns></returns>
        public SessionInfo this[string cookieValue]
        {
            get
            {
                lock (SessionsSyncLock)
                {
                    return m_Sessions.ContainsKey(cookieValue) ? m_Sessions[cookieValue] : null;
                }
            }
        }

        /// <summary>
        /// Gets a session object for the given server context.
        /// If no session exists for the context, then null is returned
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public SessionInfo GetSession(HttpListenerContext context)
        {
            lock (SessionsSyncLock)
            {
                if (context.Request.Cookies[SessionCookieName] == null) return null;

                var cookieValue = context.Request.Cookies[SessionCookieName].Value;
                return this[cookieValue];
            }

        }

        /// <summary>
        /// Gets the session.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
#if NET46
        public SessionInfo GetSession(System.Net.WebSockets.WebSocketContext context)
#else
        public SessionInfo GetSession(Unosquare.Net.WebSocketContext context)
#endif
        {
            lock (SessionsSyncLock)
            {
                if (context.CookieCollection[SessionCookieName] == null) return null;

                var cookieValue = context.CookieCollection[SessionCookieName].Value;
                return this[cookieValue];
            }
        }

        /// <summary>
        /// Gets or sets the expiration.
        /// By default, expiration is 30 minutes
        /// </summary>
        /// <value>
        /// The expiration.
        /// </value>
        public TimeSpan Expiration { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Gets or sets the cookie path.
        /// If left empty, a cookie will be created for each path. The default value is "/"
        /// If a route is specified, then session cookies will be created only for the given path.
        /// Examples of this are:
        ///     "/"
        ///     "/app1/"
        /// </summary>
        /// <value>
        /// The cookie path.
        /// </value>
        public string CookiePath { get; set; } = "/";

        /// <summary>
        /// Gets the name of this module.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public override string Name => nameof(LocalSessionModule).Humanize();
    }
}