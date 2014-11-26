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
        public SessionInfo()
        {
            this.Data = new ConcurrentDictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
        }
        public string SessionId { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime LastActivity { get; set; }
        public ConcurrentDictionary<string, object> Data { get; protected set; }

        public object this[string key]
        {
            get
            {
                return (Data.ContainsKey(key)) ? Data[key] : null;
            }
            set
            {
                Data.TryAdd(key, value);
            }
        }
    }

}
