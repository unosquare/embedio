using System.Net;
using System.Threading.Tasks;

namespace EmbedIO.Security
{
    public interface IIPBanningCriterion
    {
        Task UpdateData(IPAddress address);

        void PurgeData();
    }
}
