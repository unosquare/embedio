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
            Data = new ConcurrentDictionary<string, object>(Constants.StandardStringComparer);
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
        /// Current Session Data Repository
        /// </summary>
        public ConcurrentDictionary<string, object> Data { get; protected set; }

        /// <summary>
        /// Retrieve an item or set an item. If the key does not exist, it returns null.
        /// This is an indexer providing a shortcut to the underlying Data dictionary
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object this[string key]
        {
            get { return (Data.ContainsKey(key)) ? Data[key] : null; }
            set { Data[key] = value; }
        }
    }
}
