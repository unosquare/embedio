using System.Threading.Tasks;

namespace EmbedIO.WebApi
{
    /// <summary>
    /// <para>Represents an attribute, applied to a parameter of a web API controller method,
    /// that causes the parameter to be passed deserialized data from a request.</para>
    /// <para>The difference between this interface and <see cref="INonNullRequestDataAttribute{TController,TData}"/>
    /// is that attributes implementing this interface can retrieve <see langword="null"/> values.</para>
    /// </summary>
    /// <typeparam name="TController">The type of the controller.</typeparam>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <seealso cref="IRequestDataAttribute{TController}"/>
    /// <seealso cref="INonNullRequestDataAttribute{TController,TData}"/>
    public interface IRequestDataAttribute<in TController, TData>
        where TController : WebApiController
        where TData : class
    {
        /// <summary>
        /// Asynchronously obtains data from a controller's context.
        /// </summary>
        /// <param name="controller">The controller.</param>
        /// <param name="parameterName">The name of the parameter that has to receive the data.</param>
        /// <returns>a <see cref="Task"/> whose result will be the data
        /// to pass as a parameter to a controller method.</returns>
        Task<TData?> GetRequestDataAsync(TController controller, string parameterName);
    }
}