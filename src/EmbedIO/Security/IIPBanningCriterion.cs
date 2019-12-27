using System.Net;
using System.Threading.Tasks;

namespace EmbedIO.Security
{
    public interface IIPBanningCriterion
    {
        Task UpdateBlacklist(IPAddress address);

        void PurgeData();
    }
}
