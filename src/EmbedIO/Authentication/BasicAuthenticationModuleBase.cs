using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO.Authentication
{
    /// <summary>
    /// Basic HTTP authorization module that will return 401 + WWW-Authenticate header
    /// if a request contains invalid or no credentials.
    /// </summary>
    public abstract class BasicAuthenticationModuleBase : WebModuleBase
    {
        private readonly string _wwwAuthenticateHeaderValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicAuthenticationModuleBase"/> class.
        /// </summary>
        /// <param name="baseUrlPath">The base URL path.</param>
        /// <param name="realm">The authentication realm.</param>
        /// <remarks>
        /// <para>If <paramref name="realm"/> is <see langword="null"/> or the empty string,
        /// the <see cref="Realm"/> property will be set equal to
        /// <see cref="IWebModule.BaseUrlPath">BaseUrlPath</see>.</para>
        /// </remarks>
        public BasicAuthenticationModuleBase(string baseUrlPath, string realm)
            : base(baseUrlPath)
        {
            Realm = string.IsNullOrEmpty(realm) ? BaseUrlPath : realm;

            _wwwAuthenticateHeaderValue = $"Basic realm=\"{Realm}\" charset=UTF-8";
        }
        
        /// <summary>
        /// Gets the authentication realm.
        /// </summary>
        public string Realm { get; }

        /// <inheritdoc />
        protected override async Task<bool> OnRequestAsync(IHttpContext context, string path, CancellationToken cancellationToken)
        {
            async Task<bool> IsAuthenticatedAsync()
            {
                try
                {
                    var (userName, password) = GetCredentials(context.Request);
                    return await VerifyCredentialsAsync(path, userName, password, cancellationToken).ConfigureAwait(false);
                }
                catch (FormatException)
                {
                    // Credentials were not formatted correctly.
                    return false;
                }
            }

            if (await IsAuthenticatedAsync().ConfigureAwait(false))
            {
                context.Response.AddHeader(HttpHeaderNames.WWWAuthenticate, _wwwAuthenticateHeaderValue);
                return false;
            }

            context.Response.SetEmptyResponse((int)HttpStatusCode.Unauthorized);
            return true;
        }

        /// <summary>
        /// Verifies the credentials given in the <c>Authentication</c> request header.
        /// </summary>
        /// <param name="path">The URL path requested by the client. Note that this is relative
        /// to the module's <see cref="WebModuleBase.BaseUrlPath">BaseUrlPath</see>.</param>
        /// <param name="userName">The user name, or <see langword="null" /> if none has been given.</param>
        /// <param name="password">The password, or <see langword="null" /> if none has been given.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> use to cancel the operation.</param>
        /// <returns>
        ///   <see langword="true" /> if the given credentials are valid; otherwise, <see langword="false" />.
        /// </returns>
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
                credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Substring(6).Trim()));
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