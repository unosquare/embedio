using System.Net;
using System.Threading.Tasks;

namespace EmbedIO.Security
{
    /// <summary>
    /// Represents a criterion for <see cref="IPBanningModule"/>.
    /// </summary>
    public interface IIPBanningCriterion
    {
        /// <summary>
        /// Validates the IP address should be banned or not.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <returns><c>true</c> if the IP Address should be banned, otherwise <c>false</c>.</returns>
        Task<bool> ValidateIPAddress(IPAddress address);

        /// <summary>
        /// Purges the data of the Criterion.
        /// </summary>
        void PurgeData();
    }
}
