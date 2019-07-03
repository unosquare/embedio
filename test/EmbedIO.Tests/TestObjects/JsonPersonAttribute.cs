using System;
using System.Threading.Tasks;
using EmbedIO.WebApi;

namespace EmbedIO.Tests.TestObjects
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class JsonPersonAttribute : Attribute, IRequestDataAttribute<WebApiController, Person>
    {
        public Task<Person> GetRequestDataAsync(WebApiController controller)
            => controller.HttpContext.GetRequestDataAsync(RequestDeserializer.Json<Person>, controller.CancellationToken);
    }
}