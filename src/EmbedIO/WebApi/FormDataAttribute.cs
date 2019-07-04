using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EmbedIO.WebApi
{
    /// <summary>
    /// Specified that a parameter of a controller method will receive an <see cref="IReadOnlyDictionary{TKey,TValue}"/>
    /// of HTML form data, obtained by deserializing a request body with a content type of <c>application/x-www-form-urlencoded</c>.
    /// This class cannot be inherited.
    /// </summary>
    /// <seealso cref="Attribute" />
    /// <seealso cref="IRequestDataAttribute{TController,TData}" />
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class FormDataAttribute : Attribute, IRequestDataAttribute<WebApiController, IReadOnlyDictionary<string, object>>
    {
        /// <inheritdoc />
        public Task<IReadOnlyDictionary<string, object>> GetRequestDataAsync(WebApiController controller)
            => controller.HttpContext.GetRequestFormDataAsync(controller.CancellationToken);
    }
}