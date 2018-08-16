﻿namespace Unosquare.Labs.EmbedIO
{
    using Constants;
    using System;
    using System.Collections.Concurrent;

    /// <summary>
    /// Represents the contents of an HTTP Session.
    /// </summary>
    public class SessionInfo
    {
        private readonly Lazy<ConcurrentDictionary<string, object>> _lazyData =
            new Lazy<ConcurrentDictionary<string, object>>(() =>
                new ConcurrentDictionary<string, object>(Strings.StandardStringComparer));
        
        /// <summary>
        /// Current Session Identifier.
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Gets or sets the date created.
        /// </summary>
        /// <value>
        /// The date created.
        /// </value>
        public DateTime DateCreated { get; set; }

        /// <summary>
        /// Gets or sets the last activity.
        /// </summary>
        /// <value>
        /// The last activity.
        /// </value>
        public DateTime LastActivity { get; set; }

        /// <summary>
        /// Current Session Data Repository.
        /// </summary>
        public ConcurrentDictionary<string, object> Data => _lazyData.Value;

        /// <summary>
        /// Retrieve an item or set an item. If the key does not exist, it returns null.
        /// This is an indexer providing a shortcut to the underlying Data dictionary.
        /// </summary>
        /// <param name="key">The key as an indexer.</param>
        /// <returns>An object that represents current session data repository.</returns>
        public object this[string key]
        {
            get => Data.ContainsKey(key) ? Data[key] : null;
            set => Data[key] = value;
        }
    }
}
