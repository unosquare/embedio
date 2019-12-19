using System.Net;

namespace EmbedIO.Security
{
    /// <summary>
    /// Represents the info af a banned IP address.
    /// </summary>
    public class BannedInfo
    {
        /// <summary>
        /// Gets or sets the banned IP address.
        /// </summary>
        public IPAddress IPAddress { get; set; }

        /// <summary>
        /// Gets or sets until when the IP will remain ban.
        /// </summary>
        public long BanUntil { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance was explicitly banned by user.
        /// </summary>
        public bool IsExplicit { get; set; }
    }
}
