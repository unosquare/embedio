using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Internal;

namespace EmbedIO
{
    /// <summary>
    /// Provides standard request parsing callbacks.
    /// </summary>
    public static class RequestParser
    {
        /// <summary>
        /// Asynchronously parses a request body in <c>application/x-www-form-urlencoded</c> format.
        /// </summary>
        /// <param name="context">The <see cref="IHttpContext"/> whose request body is to be parsed.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="Task{TResult}">Task</see>, representing the ongoing operation,
        /// whose result will be a <see cref="Dictionary{TKey,TValue}">Dictionary</see> of form field names and values.</returns>
        public static async Task<Dictionary<string, object>> UrlEncodedFormData(IHttpContext context, CancellationToken cancellationToken)
        {
            using (var reader = context.OpenRequestText())
            {
                return FormDataParser.ParseAsDictionary(await reader.ReadToEndAsync().ConfigureAwait(false));
            }
        }
    }
}