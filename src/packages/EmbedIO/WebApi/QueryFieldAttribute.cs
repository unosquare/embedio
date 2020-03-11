using System;
using System.Linq;
using System.Threading.Tasks;
using EmbedIO.Utilities;
using Swan;

namespace EmbedIO.WebApi
{
    /// <summary>
    /// <para>Specifies that a parameter of a controller method will receive the value of a field,
    /// obtained by deserializing a request URL query.</para>
    /// <para>The parameter carrying this attribute can be either a simple type or a one-dimension array.</para>
    /// <para>If multiple values are present for the field, a non-array parameter will receive the last specified value,
    /// while an array parameter will receive an array of field values converted to the element type of the
    /// parameter.</para>
    /// <para>If a single value is present for the field, a non-array parameter will receive the value converted
    /// to the type of the parameter, while an array parameter will receive an array of length 1, containing
    /// the value converted to the element type of the parameter</para>
    /// <para>If no values are present for the field and the <see cref="BadRequestIfMissing"/> property is
    /// <see langword="true" />, a <c>400 Bad Request</c> response will be sent to the client, with a message
    /// specifying the name of the missing field.</para>
    /// <para>If no values are present for the field and the <see cref="BadRequestIfMissing"/> property is
    /// <see langword="false" />, a non-array parameter will receive the default value for its type, while
    /// an array parameter will receive an array of length 0.</para>
    /// <para>This class cannot be inherited.</para>
    /// </summary>
    /// <seealso cref="Attribute" />
    /// <seealso cref="IRequestDataAttribute{TController}" />
    /// <seealso cref="IRequestDataAttribute{TController,TData}" />
    /// <seealso cref="INonNullRequestDataAttribute{TController,TData}" />
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class QueryFieldAttribute :
        Attribute,
        IRequestDataAttribute<WebApiController, string>,
        INonNullRequestDataAttribute<WebApiController, string[]>,
        IRequestDataAttribute<WebApiController>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryFieldAttribute"/> class.
        /// The name of the query field to extract will be equal to the name of the parameter
        /// carrying this attribute.
        /// </summary>
        public QueryFieldAttribute()
            : this(false, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryFieldAttribute"/> class.
        /// </summary>
        /// <param name="fieldName">The name of the query field to extract.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fieldName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="fieldName"/> is the empty string (<c>""</c>).</exception>
        public QueryFieldAttribute(string fieldName)
            : this(false, Validate.NotNullOrEmpty(nameof(fieldName), fieldName))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryFieldAttribute" /> class.
        /// The name of the query field to extract will be equal to the name of the parameter
        /// carrying this attribute.
        /// </summary>
        /// <param name="badRequestIfMissing">If set to <see langword="true" />, a <c>400 Bad Request</c>
        /// response will be sent to the client if no values are found for the field; if set to
        /// <see langword="false" />, a default value will be assumed.</param>
        public QueryFieldAttribute(bool badRequestIfMissing)
            : this(badRequestIfMissing, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryFieldAttribute"/> class.
        /// </summary>
        /// <param name="fieldName">The name of the query field to extract.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fieldName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="fieldName"/> is the empty string (<c>""</c>).</exception>
        /// <param name="badRequestIfMissing">If set to <see langword="true" />, a <c>400 Bad Request</c>
        /// response will be sent to the client if no values are found for the field; if set to
        /// <see langword="false" />, a default value will be assumed.</param>
        public QueryFieldAttribute(string fieldName, bool badRequestIfMissing)
            : this(badRequestIfMissing, Validate.NotNullOrEmpty(nameof(fieldName), fieldName))
        {
        }

        private QueryFieldAttribute(bool badRequestIfMissing, string? fieldName)
        {
            BadRequestIfMissing = badRequestIfMissing;
            FieldName = fieldName;
        }

        /// <summary>
        /// Gets the name of the query field that this attribute will extract,
        /// or <see langword="null" /> if the name of the parameter carrying this
        /// attribute is to be used as field name.
        /// </summary>
        public string? FieldName { get; }

        /// <summary>
        /// <para>Gets or sets a value indicating whether to send a <c>400 Bad Request</c> response
        /// to the client if the URL query contains no values for the field.</para>
        /// <para>If this property is <see langword="true"/> and the URL query
        /// contains no values for the field, the <c>400 Bad Request</c> response sent
        /// to the client will contain a reference to the missing field.</para>
        /// <para>If this property is <see langword="false"/> and the URL query
        /// contains no values for the field, the default value for the parameter
        /// (or a zero-length array if the parameter is of an array type)
        /// will be passed to the controller method.</para>
        /// </summary>
        public bool BadRequestIfMissing { get; }

        Task<string?> IRequestDataAttribute<WebApiController, string>.GetRequestDataAsync(
            WebApiController controller,
            string parameterName)
        {
            var data = controller.HttpContext.GetRequestQueryData();

            var fieldName = FieldName ?? parameterName;
            if (!data.ContainsKey(fieldName) && BadRequestIfMissing)
                throw HttpException.BadRequest($"Missing query field {fieldName}.");

            return Task.FromResult(data.GetValues(fieldName)?.LastOrDefault());
        }

        Task<string[]> INonNullRequestDataAttribute<WebApiController, string[]>.GetRequestDataAsync(
            WebApiController controller,
            string parameterName)
        {
            var data = controller.HttpContext.GetRequestQueryData();

            var fieldName = FieldName ?? parameterName;
            if (!data.ContainsKey(fieldName) && BadRequestIfMissing)
                throw HttpException.BadRequest($"Missing query field {fieldName}.");

            return Task.FromResult(data.GetValues(fieldName) ?? Array.Empty<string>());
        }

        Task<object?> IRequestDataAttribute<WebApiController>.GetRequestDataAsync(
            WebApiController controller,
            Type type,
            string parameterName)
        {
            var data = controller.HttpContext.GetRequestQueryData();

            var fieldName = FieldName ?? parameterName;
            if (!data.ContainsKey(fieldName) && BadRequestIfMissing)
                throw HttpException.BadRequest($"Missing query field {fieldName}.");

            object result = null;
            if (type.IsArray)
            {
                var fieldValues = data.GetValues(fieldName) ?? Array.Empty<string>();
                if (!FromString.TryConvertTo(type, fieldValues, out result))
                    throw HttpException.BadRequest($"Cannot convert field {fieldName} to an array of {type.GetElementType().Name}.");

                return Task.FromResult(result);
            }
            else
            {
                var fieldValue = data.GetValues(fieldName)?.LastOrDefault();
                if (fieldValue == null)
                {
                    if (type.IsValueType)
                    {
                        var parameter = controller.CurrentMethod.GetParameters().FirstOrDefault(p => p.Name == parameterName);
                        result = parameter.HasDefaultValue ? parameter.DefaultValue : Activator.CreateInstance(type);
                    }

                    return Task.FromResult(result);
                }

                if (!FromString.TryConvertTo(type, fieldValue, out result))
                    throw HttpException.BadRequest($"Cannot convert field {fieldName} to {type.Name}.");

                return Task.FromResult(result);
            }
        }
    }
}