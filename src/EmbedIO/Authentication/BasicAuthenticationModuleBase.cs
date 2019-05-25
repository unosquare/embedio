using System;
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
        /// <value>
        /// The authentication realm.
        /// </value>
        public string Realm { get; }

        /// <inheritdoc />
        public override async Task<bool> HandleRequestAsync(IHttpContext context, string path, CancellationToken cancellationToken)
        {
            try
            {
                var (userName, password) = GetCredentials(context.Request);

                if (!await VerifyCredentialsAsync(userName, password).ConfigureAwait(false))
                    context.Response.StatusCode = 401;
            }
            catch (FormatException)
            {
                // Credentials were not formatted correctly.
                context.Response.StatusCode = 401;
            }

            if (context.Response.StatusCode == 401)
                return false;

            context.Response.AddHeader("WWW-Authenticate", _wwwAuthenticateHeaderValue);
            return true;
        }

        /// <summary>
        /// Verifies the credentials given in the <c>Authentication</c> request header.
        /// </summary>
        /// <param name="userName">The user name.</param>
        /// <param name="password">The password.</param>
        /// <returns><see langword="true"/> if the credentials are valid; otherwise, <see langword="false"/>.</returns>
        protected abstract Task<bool> VerifyCredentialsAsync(string userName, string password);

        private static (string UserName, string Password) GetCredentials(IHttpRequest request)
        {
            var authHeader = request.Headers["Authorization"];

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