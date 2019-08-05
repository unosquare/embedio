using System;
using Swan.Collections;

namespace EmbedIO.Sessions
{
    /// <summary>
    /// Represents a session.
    /// </summary>
    /// <seealso cref="IDataDictionary{TKey,TValue}"/>
    public interface ISession : IDataDictionary<string, object>
    {
        /// <summary>
        /// A unique identifier for the session.
        /// </summary>
        /// <value>The unique identifier for this session.</value>
        /// <seealso cref="Session.IdComparison"/>
        /// <seealso cref="Session.IdComparer"/>
        string Id { get; }

        /// <summary>
        /// Gets the time interval, starting from <see cref="LastActivity"/>,
        /// after which the session expires.
        /// </summary>
        /// <value> The expiration time.</value>
        TimeSpan Duration { get; }

        /// <summary>
        /// Gets the UTC date and time of last activity on the session.
        /// </summary>
        /// <value>
        /// The UTC date and time of last activity on the session.
        /// </value>
        DateTime LastActivity { get; }
    }
}