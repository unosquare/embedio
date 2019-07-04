using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EmbedIO.WebApi
{
    /// <summary>
    /// Specified that a parameter of a controller method will receive a <see cref="Dictionary{TKey,TValue}">Dictionary</see>
    /// of HTML form data, obtained by deserializing a request body with a content type of <c>application/x-www-form-urlencoded</c>.
    /// This class cannot be inherited.
    /// </summary>
    /// <seealso cref="Attribute" />
    /// <seealso cref="IRequestDataAttribute{TController,TData}" />
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class FormDataAttribute : Attribute, IRequestDataAttribute<WebApiController, Dictionary<string, object>>
    {
        /// <inheritdoc />
        public Task<Dictionary<string, object>> GetRequestDataAsync(WebApiController controller)
            => controller.HttpContext.GetRequestFormDataAsync(controller.CancellationToken);
    }
}