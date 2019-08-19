using System;
using System.Threading.Tasks;
using EmbedIO.WebApi;

namespace EmbedIO.Tests.TestObjects
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class JsonDataAttribute : Attribute, IRequestDataAttribute<WebApiController>
    {
        public async Task<object> GetRequestDataAsync(WebApiController controller, Type type, string parameterName)
        {
            string body;
            using (var reader = controller.HttpContext.OpenRequestText())
            {
                body = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            try
            {
                return Swan.Formatters.Json.Deserialize(body, type);
            }
            catch (FormatException)
            {
                throw HttpException.BadRequest($"Expected request body to be deserializable to {type.FullName}.");
            }
        }
    }
}