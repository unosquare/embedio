using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EmbedIO.WebApi
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class FormDataAttribute : Attribute, IRequestDataAttribute<WebApiController, Dictionary<string, object>>
    {
        public Task<Dictionary<string, object>> GetRequestDataAsync(WebApiController controller)
            => controller.HttpContext.GetRequestFormDataAsync(controller.CancellationToken);
    }
}