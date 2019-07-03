using System.Threading.Tasks;

namespace EmbedIO.WebApi
{
    public interface IRequestDataAttribute<in TController, TData>
        where TController : WebApiController
    {
        Task<TData> GetRequestDataAsync(TController controller);
    }
}