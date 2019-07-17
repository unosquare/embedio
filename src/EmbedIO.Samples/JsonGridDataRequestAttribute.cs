using System;
using System.Threading.Tasks;
using EmbedIO.WebApi;
using Unosquare.Tubular.ObjectModel;

namespace EmbedIO.Samples
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class JsonGridDataRequestAttribute : Attribute, IRequestDataAttribute<WebApiController, GridDataRequest>
    {
        public Task<GridDataRequest> GetRequestDataAsync(WebApiController controller, string parameterName)
            => controller.HttpContext.GetRequestDataAsync(RequestDeserializer.Json<GridDataRequest>, controller.CancellationToken);
    }
}