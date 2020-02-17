using System;
using System.Threading.Tasks;
using EmbedIO.WebApi;
using Unosquare.Tubular;

namespace EmbedIO.Samples
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class JsonGridDataRequestAttribute : Attribute, INonNullRequestDataAttribute<WebApiController, GridDataRequest>
    {
        public Task<GridDataRequest> GetRequestDataAsync(WebApiController controller, string parameterName)
            => controller.HttpContext.GetRequestDataAsync(RequestDeserializer.Json<GridDataRequest>);
    }
}