using System.Globalization;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace EmbedIO
{
    /// <summary>
    /// Provides extension methods for types implementing <see cref="IHttpRequest"/>.
    /// </summary>
    public static class HttpRequestExtensions
    {
        /// <summary>
        /// <para>Returns a string representing the remote IP address and port of an <see cref="IHttpRequest"/> interface.</para>
        /// <para>This method can be called even on a <see langword="null"/> interface, or one that has no
        /// remote end point, or no remote address; it will always return a non-<see langword="null"/>,
        /// non-empty string.</para>
        /// </summary>
        /// <param name="this">The <see cref="IHttpRequest"/> on which this method is called.</param>
        /// <returns>
        /// If <paramref name="this"/> is <see langword="null"/>, or its <see cref="IHttpRequest.RemoteEndPoint">RemoteEndPoint</see>
        /// is <see langword="null"/>, the string <c>"&lt;null&gt;</c>; otherwise, the remote end point's
        /// <see cref="IPEndPoint.Address">Address</see> (or the string <c>"&lt;???&gt;"</c> if it is <see langword="null"/>)
        /// followed by a colon and the <see cref="IPEndPoint.Port">Port</see> number.
        /// </returns>
        public static string SafeGetRemoteEndpointStr(this IHttpRequest @this)
        {
            var endPoint = @this?.RemoteEndPoint;
            if (endPoint == null)
                return "<null>";

            return $"{endPoint.Address?.ToString() ?? "<???>"}:{endPoint.Port.ToString(CultureInfo.InvariantCulture)}";
        }

        /// <summary>
        /// Retrieves the request body as a string.
        /// Note that once this method returns, the underlying input stream cannot be read again as
        /// it is not rewindable for obvious reasons. This functionality is by design.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>
        /// A task with the rest of the stream as a string, from the current position to the end.
        /// If the current position is at the end of the stream, returns an empty string.
        /// </returns>
        public static async Task<string> GetBodyAsStringAsync(this IHttpRequest request)
        {
            if (!request.HasEntityBody)
                return null;

            using (var body = request.InputStream)
            using (var reader = new StreamReader(body, request.ContentEncoding))
            {
                return await reader.ReadToEndAsync().ConfigureAwait(false);
            }
        }
    }
}