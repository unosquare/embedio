using System;
using System.Threading.Tasks;
using EmbedIO.Utilities;

namespace EmbedIO.WebApi
{
    /// <summary>
    /// <para>Specifies that a parameter of a controller method will receive the value of a field,
    /// obtained by deserializing a request URL query.</para>
    /// <para>The parameter carrying this attribute can be either a <see cref="string"/>, or an array of strings.</para>
    /// <para>A <see cref="string"/> parameter will receive multiple field values (if any) as a comma-separated list.</para>
    /// <para>An array parameter will receive a single field value as an array of one string.</para>
    /// <para>This class cannot be inherited.</para>
    /// </summary>
    /// <seealso cref="Attribute" />
    /// <seealso cref="IRequestDataAttribute{TController,TData}" />
    /// <seealso cref="IRequestDataAttribute{TController}" />
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class QueryFieldAttribute : 
        Attribute,
        IRequestDataAttribute<WebApiController, string>,
        IRequestDataAttribute<WebApiController, string[]>,
        IRequestDataAttribute<WebApiController>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryFieldAttribute"/> class.
        /// </summary>
        /// <param name="fieldName">The name of the query field to extract.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fieldName"/> is <see langword="null"/>.</exception>
        public QueryFieldAttribute(string fieldName)
        {
            FieldName = Validate.NotNull(nameof(fieldName), fieldName);
        }

        /// <summary>
        /// Gets the name of the form field that this attribute will extract.
        /// </summary>
        public string FieldName { get; }

        /// <summary>
        /// <para>Gets or sets a value indicating whether to send a <c>400 Bad Request</c> response
        /// to the client if the form field specified by the <see cref="FieldName"/> property
        /// is missing in the URL query.</para>
        /// <para>If this property is <see langword="true"/> and the URL query
        /// contains no field whose name corresponds to <see cref="FieldName"/>,
        /// the <c>400 Bad Request</c> response sent to the client will contain
        /// a reference to the missing field.</para>
        /// <para>If this property is <see langword="false"/> and the URL query
        /// contains no field whose name corresponds to <see cref="FieldName"/>,
        /// the default value for the parameter will be passed to the controller method.</para>
        /// </summary>
        public bool BadRequestIfMissing { get; set; }

        Task<string> IRequestDataAttribute<WebApiController, string>.GetRequestDataAsync(WebApiController controller)
        {
            var queryData = controller.HttpContext.GetRequestQueryData();

            if (!queryData.ContainsKey(FieldName) && BadRequestIfMissing)
                throw HttpException.BadRequest($"Missing query field {FieldName}.");

            return Task.FromResult(queryData.Get(FieldName));
        }

        Task<string[]> IRequestDataAttribute<WebApiController, string[]>.GetRequestDataAsync(WebApiController controller)
        {
            var queryData = controller.HttpContext.GetRequestQueryData();

            if (!queryData.ContainsKey(FieldName) && BadRequestIfMissing)
                throw HttpException.BadRequest($"Missing query field {FieldName}.");

            return Task.FromResult(queryData.GetValues(FieldName));
        }

        Task<object> IRequestDataAttribute<WebApiController>.GetRequestDataAsync(WebApiController controller, Type type)
        {
            var queryData = controller.HttpContext.GetRequestQueryData();

            if (!queryData.ContainsKey(FieldName) && BadRequestIfMissing)
                throw HttpException.BadRequest($"Missing query field {FieldName}.");

            var fieldValue = queryData.Get(FieldName);
            if (!FromString.TryConvertTo(type, fieldValue, out var result))
                throw HttpException.BadRequest($"Cannot convert query field {FieldName} to {type.Name}.");

            return Task.FromResult(result);
        }
    }
}