using System;
using System.Collections.Concurrent;

namespace EmbedIO
{
    /// <summary>
    /// Represents the contents of an HTTP Session.
    /// </summary>
    public class SessionInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SessionInfo"/> class.
        /// </summary>
        /// <param name="id">The session identifier.</param>
        /// <param name="duration">The inactivity time after which the session will expire.</param>
        public SessionInfo(string id, TimeSpan duration)
        {
            Id = id;
            Duration = duration;
            LastActivity = DateTime.UtcNow;
        }

        /// <summary>
        /// A unique identifier for the session.
        /// </summary>
        /// <value>The unique identifier for this session.</value>
        public string Id { get; }

        /// <summary>
        /// Gets the time interval, starting from <see cref="LastActivity"/>,
        /// after which the session expires.
        /// </summary>
        /// <value> The expiration time.</value>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Gets or sets the UTC date and time of last activity on the session.
        /// </summary>
        /// <value>
        /// The last activity.
        /// </value>
        public DateTime LastActivity { get; set; }

        /// <summary>
        /// Gets a value indicating whether a session is expired.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if this session is expired; otherwise, <see langword="false"/> .
        /// </value>
        public bool IsExpired => DateTime.UtcNow > LastActivity + Duration;

        /// <summary>
        /// Current Session Data Repository.
        /// </summary>
        public ConcurrentDictionary<string, object> Data { get; } = new ConcurrentDictionary<string, object>(StringComparer.InvariantCulture);

        /// <summary>
        /// Retrieve an item or set an item. If the key does not exist, it returns null.
        /// This is an indexer providing a shortcut to the underlying Data dictionary.
        /// </summary>
        /// <param name="key">The key as an indexer.</param>
        /// <returns>An object that represents current session data repository.</returns>
        public object this[string key]
        {
            get => Data.TryGetValue(key, out var value) ? value : null;
            set => Data[key] = value;
        }
    }
}