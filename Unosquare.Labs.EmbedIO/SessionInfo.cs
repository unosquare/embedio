namespace Unosquare.Labs.EmbedIO
{
    using System;
    using System.Collections.Concurrent;

    /// <summary>
    /// Represents the contents of an HTTP Session
    /// </summary>
    public class SessionInfo
    {
        /// <summary>
        /// Initialize Session data
        /// </summary>
        public SessionInfo()
        {
            Data = new ConcurrentDictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Current Session Identifier
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Current Session creation date
        /// </summary>
        public DateTime DateCreated { get; set; }

        /// <summary>
        /// Current Session last activity date
        /// </summary>
        public DateTime LastActivity { get; set; }

        /// <summary>
        /// Current Session Data Repo
        /// </summary>
        public ConcurrentDictionary<string, object> Data { get; protected set; }

        /// <summary>
        /// Retrieve an item. If the key does not exist, it return null.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object this[string key]
        {
            get { return (Data.ContainsKey(key)) ? Data[key] : null; }
            set { Data.TryAdd(key, value); }
        }
    }
}