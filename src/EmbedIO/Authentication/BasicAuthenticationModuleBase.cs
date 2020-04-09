using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO.Authentication
{
    /// <summary>
    /// Implements <see href="https://tools.ietf.org/html/rfc7617">HTTP basic authentication</see>.
    /// </summary>
    public abstract class BasicAuthenticationModuleBase : WebModuleBase
    {
        private readonly string _wwwAuthenticateHeaderValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicAuthenticationModuleBase"/> class.
        /// </summary>
        /// <param name="baseRoute">The base URL path.</param>
        /// <param name="realm">The authentication realm.</param>
        /// <remarks>
        /// <para>If <paramref name="realm"/> is <see langword="null"/> or the empty string,
        /// the <see cref="Realm"/> property will be set equal to
        /// <see cref="IWebModule.BaseRoute">BaseRoute</see>.</para>
        /// </remarks>
        protected BasicAuthenticationModuleBase(string baseRoute, string? realm)
            : base(baseRoute)
        {
            Realm = string.IsNullOrEmpty(realm) ? BaseRoute : realm;

            _wwwAuthenticateHeaderValue = $"Basic realm=\"{Realm}\" charset=UTF-8";
        }

        /// <inheritdoc />
        public sealed override bool IsFinalHandler => false;

        /// <summary>
        /// Gets the authentication realm.
        /// </summary>
        public string Realm { get; }

        /// <inheritdoc />
        protected sealed override async Task OnRequestAsync(IHttpContext context)
        {
            async Task<bool> IsAuthenticatedAsync()
            {
                try
                {
                    var (userName, password) = GetCredentials(context.Request);
                    return await VerifyCredentialsAsync(context.RequestedPath, userName, password, context.CancellationToken)
                        .ConfigureAwait(false);
                }
                catch (FormatException)
                {
                    // Credentials were not formatted correctly.
                    return false;
                }
            }

            if (!await IsAuthenticatedAsync().ConfigureAwait(false))
                throw HttpException.Unauthorized();

            context.Response.Headers.Set(HttpHeaderNames.WWWAuthenticate, _wwwAuthenticateHeaderValue);
        }

        /// <summary>
        /// Verifies the credentials given in the <c>Authentication</c> request header.
        /// </summary>
        /// <param name="path">The URL path requested by the client. Note that this is relative
        /// to the module's <see cref="WebModuleBase.BaseRoute">BaseRoute</see>.</param>
        /// <param name="userName">The user name, or <see langword="null" /> if none has been given.</param>
        /// <param name="password">The password, or <see langword="null" /> if none has been given.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> use to cancel the operation.</param>
        /// <returns>A <see cref="Task{TResult}"/> whose result will be <see langword="true" /> if the given credentials
        /// are valid, <see langword="false" /> if they are not.</returns>
        protected abstract Task<bool> VerifyCredentialsAsync(string path, string userName, string password, CancellationToken cancellationToken);

        private static (string UserName, string Password) GetCredentials(IHttpRequest request)
        {
            var authHeader = request.Headers[HttpHeaderNames.Authorization];

            if (authHeader == null)
                return default;

            if (!authHeader.StartsWith("basic ", StringComparison.OrdinalIgnoreCase))
                return default;

            string credentials;
            try
            {
                credentials = WebServer.DefaultEncoding.GetString(Convert.FromBase64String(authHeader.Substring(6).Trim()));
            }
            catch (FormatException)
            {
                return default;
            }

            var separatorPos = credentials.IndexOf(':');
            return separatorPos < 0
                ? (credentials, string.Empty)
                : (credentials.Substring(0, separatorPos), credentials.Substring(separatorPos + 1));
        }
    }
}