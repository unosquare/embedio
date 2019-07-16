using System;
using System.Threading.Tasks;
using EmbedIO.Utilities;

namespace EmbedIO.WebApi
{
    /// <summary>
    /// <para>Specifies that a parameter of a controller method will receive the value of a field in a HTML form,
    /// obtained by deserializing a request body with a content type of <c>application/x-www-form-urlencoded</c>.</para>
    /// <para>The parameter carrying this attribute can be either a <see cref="string"/>, or an array of strings.</para>
    /// <para>A <see cref="string"/> parameter will receive multiple field values (if any) as a comma-separated list.</para>
    /// <para>An array parameter will receive a single field value as an array of one string.</para>
    /// <para>This class cannot be inherited.</para>
    /// </summary>
    /// <seealso cref="Attribute" />
    /// <seealso cref="IRequestDataAttribute{TController,TData}" />
    /// <seealso cref="IRequestDataAttribute{TController}" />
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class FormFieldAttribute : 
        Attribute,
        IRequestDataAttribute<WebApiController, string>,
        IRequestDataAttribute<WebApiController, string[]>,
        IRequestDataAttribute<WebApiController>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FormFieldAttribute"/> class.
        /// </summary>
        /// <param name="fieldName">The name of the form field to extract.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fieldName"/> is <see langword="null"/>.</exception>
        public FormFieldAttribute(string fieldName)
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
        /// is missing in the submitted data.</para>
        /// <para>If this property is <see langword="true"/> and the submitted form
        /// contains no field whose name corresponds to <see cref="FieldName"/>,
        /// the <c>400 Bad Request</c> response sent to the client will contain
        /// a reference to the missing field.</para>
        /// <para>If this property is <see langword="false"/> and the submitted form
        /// contains no field whose name corresponds to <see cref="FieldName"/>,
        /// the default value for the parameter will be passed to the controller method.</para>
        /// </summary>
        public bool BadRequestIfMissing { get; set; }

        async Task<string> IRequestDataAttribute<WebApiController, string>.GetRequestDataAsync(WebApiController controller)
        {
            var formData = await controller.HttpContext.GetRequestFormDataAsync(controller.CancellationToken)
                .ConfigureAwait(false);

            if (!formData.ContainsKey(FieldName) && BadRequestIfMissing)
                throw HttpException.BadRequest($"Missing form field {FieldName}.");

            return formData.Get(FieldName);
        }

        async Task<string[]> IRequestDataAttribute<WebApiController, string[]>.GetRequestDataAsync(WebApiController controller)
        {
            var formData = await controller.HttpContext.GetRequestFormDataAsync(controller.CancellationToken)
                .ConfigureAwait(false);

            if (!formData.ContainsKey(FieldName) && BadRequestIfMissing)
                throw HttpException.BadRequest($"Missing form field {FieldName}.");

            return formData.GetValues(FieldName);
        }

        async Task<object> IRequestDataAttribute<WebApiController>.GetRequestDataAsync(WebApiController controller, Type type)
        {
            var formData = await controller.HttpContext.GetRequestFormDataAsync(controller.CancellationToken)
                .ConfigureAwait(false);

            if (!formData.ContainsKey(FieldName) && BadRequestIfMissing)
                throw HttpException.BadRequest($"Missing form field {FieldName}.");

            var fieldValue = formData.Get(FieldName);
            if (!FromString.TryConvertTo(type, fieldValue, out var result))
                throw HttpException.BadRequest($"Cannot convert field {FieldName} to {type.Name}.");

            return result;
        }
    }
}