using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using EmbedIO.Utilities;

namespace EmbedIO.WebApi
{
    /// <summary>
    /// <para>Specifies that a parameter of a controller method will receive a <see cref="NameValueCollection"/>
    /// of HTML form data, obtained by deserializing a request URL query.</para>
    /// <para>The received collection will be read-only.</para>
    /// <para>This class cannot be inherited.</para>
    /// </summary>
    /// <seealso cref="Attribute" />
    /// <seealso cref="INonNullRequestDataAttribute{TController,TData}" />
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class QueryDataAttribute : Attribute, INonNullRequestDataAttribute<WebApiController, NameValueCollection>
    {
        /// <inheritdoc />
        public Task<NameValueCollection> GetRequestDataAsync(WebApiController controller, string parameterName)
            => Task.FromResult(Validate.NotNull(nameof(controller), controller).HttpContext.GetRequestQueryData());
    }
}