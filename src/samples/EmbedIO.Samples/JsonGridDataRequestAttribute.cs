using System;
using System.Threading.Tasks;
using EmbedIO.Utilities;
using EmbedIO.WebApi;
using Unosquare.Tubular;

namespace EmbedIO.Samples
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class JsonGridDataRequestAttribute : Attribute, INonNullRequestDataAttribute<WebApiController, GridDataRequest>
    {
        public Task<GridDataRequest> GetRequestDataAsync(WebApiController controller, string parameterName)
            => Validate.NotNull(nameof(controller), controller).HttpContext.GetRequestDataAsync(RequestDeserializer.Json<GridDataRequest>);
    }
}