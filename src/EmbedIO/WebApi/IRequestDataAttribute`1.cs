using System;
using System.Threading.Tasks;

namespace EmbedIO.WebApi
{
    /// <summary>
    /// Represents an attribute, applied to a parameter of a web API controller method,
    /// that causes the parameter to be passed deserialized data from a request.
    /// </summary>
    /// <typeparam name="TController">The type of the controller.</typeparam>
    /// <seealso cref="IRequestDataAttribute{TController,TData}"/>
    public interface IRequestDataAttribute<in TController>
        where TController : WebApiController
    {
        /// <summary>
        /// Asynchronously obtains data from a controller's context.
        /// </summary>
        /// <param name="controller">The controller.</param>
        /// <param name="type">The type of the parameter that has to receive the data.</param>
        /// <param name="parameterName">The name of the parameter that has to receive the data.</param>
        /// <returns>a <see cref="Task"/> whose result will be the data
        /// to pass as a parameter to a controller method.</returns>
        Task<object?> GetRequestDataAsync(TController controller, Type type, string parameterName);
    }
}