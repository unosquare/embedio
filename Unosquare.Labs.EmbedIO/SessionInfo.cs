namespace Unosquare.Labs.EmbedIO
{
    using System;
#if PATCH_COLLECTIONS
    using Unosquare.Labs.EmbedIO.Collections.Concurrent;

#else
    using System.Collections.Concurrent;
#endif

    /// <summary>
    /// Represents a Session and its contents
    /// </summary>
    public class SessionInfo
    {
        /// <summary>
        /// Initialize Session data
        /// </summary>
        public SessionInfo()
        {
            this.Data = new ConcurrentDictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
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
        /// Retrieve an item
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