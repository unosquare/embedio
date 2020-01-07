using System.Net;

namespace EmbedIO.Security
{
    /// <summary>
    /// Contains information about the ban of an IP address.
    /// </summary>
    public class BanInfo
    {
        /// <summary>
        /// Gets or sets the banned IP address.
        /// </summary>
        public IPAddress IPAddress { get; set; }

        /// <summary>
        /// Gets or sets the expiration time of the ban.
        /// </summary>
        public long ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance was explicitly banned.
        /// </summary>
        public bool IsExplicit { get; set; }
    }
}
