using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Utilities;

namespace EmbedIO.Authentication
{
    /// <summary>
    /// Base class for authentication modules using <see href="https://tools.ietf.org/html/rfc7617">HTTP basic authentication</see>.
    /// </summary>
    public abstract class BasicAuthenticationModuleBase : AuthenticationModuleBase
    {
        /// <summary>
        /// The authentication type used by this module.
        /// </summary>
        public const string AuthenticationType = "Basic";

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
            realm ??= BaseRoute;
            if (realm.Length == 0)
                realm = BaseRoute;

            Realm = realm;

            _wwwAuthenticateHeaderValue = $"Basic realm=\"{Realm}\" charset=UTF-8";
        }

        /// <summary>
        /// Gets the authentication realm.
        /// </summary>
        public string Realm { get; }

        /// <inheritdoc />
        protected sealed override async Task<IPrincipal> AuthenticateAsync([ValidatedNotNull] IHttpContext context)
        {
            var authHeader = context.Request.Headers[HttpHeaderNames.Authorization];
            if (authHeader == null || !authHeader.StartsWith("basic ", StringComparison.OrdinalIgnoreCase))
                return Auth.NoUser;

            string credentials;
            try
            {
                credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Substring(6).Trim()));
            }
            catch (FormatException)
            {
                return Auth.CreateUnauthenticatedPrincipal(AuthenticationType);
            }

            var separatorPos = credentials.IndexOf(':');
            var (userName, password) = separatorPos < 0
                ? (credentials, string.Empty)
                : (credentials.Substring(0, separatorPos), credentials.Substring(separatorPos + 1));
            var roles = new List<string>();
            var authenticated = userName.Length > 0 && await VerifyCredentialsAsync(context.RequestedPath, userName, password, roles, context.CancellationToken)
                .ConfigureAwait(false);

            return authenticated
                ? new GenericPrincipal(new GenericIdentity(userName, AuthenticationType), roles.ToArray())
                : Auth.CreateUnauthenticatedPrincipal(AuthenticationType);
        }

        /// <summary>
        /// Verifies the credentials given in the <c>Authentication</c> request header.
        /// </summary>
        /// <param name="path">The URL path requested by the client. Note that this is relative
        /// to the module's <see cref="WebModuleBase.BaseRoute">BaseRoute</see>.</param>
        /// <param name="userName">The user name.</param>
        /// <param name="password">The password, or the empty string if none has been given.</param>
        /// <param name="roles">A list to which the user's roles can be added.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> use to cancel the operation.</param>
        /// <returns>A <see cref="Task{TResult}"/> whose result will be <see langword="true"/> if the given credentials
        /// are valid, <see langword="false"/> if they are not.</returns>
        protected abstract Task<bool> VerifyCredentialsAsync(string path, string userName, string password, IList<string> roles, CancellationToken cancellationToken);
    }
}