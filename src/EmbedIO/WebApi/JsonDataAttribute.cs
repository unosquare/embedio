using System;
using System.Threading.Tasks;

namespace EmbedIO.WebApi
{
    /// <summary>
    /// <para>Specifies that a parameter of a controller method will receive
    /// an object obtained by deserializing the request body as JSON.</para>
    /// <para>The received object will be <see langword="null"/>
    /// only if the deserialized object is <c>null</c>.</para>
    /// <para>If the request body is not valid JSON,
    /// or if it cannot be deserialized to the type of the parameter,
    /// a <c>400 Bad Request</c> response will be sent to the client.</para>
    /// <para>This class cannot be inherited.</para>
    /// </summary>
    /// <seealso cref="Attribute" />
    /// <seealso cref="IRequestDataAttribute{TController}" />
    [AttributeUsage(AttributeTargets.Parameter)]
    public class JsonDataAttribute : Attribute, IRequestDataAttribute<WebApiController>
    {
        /// <inheritdoc />
        public async Task<object?> GetRequestDataAsync(WebApiController controller, Type type, string parameterName)
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