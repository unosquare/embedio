namespace Unosquare.Labs.EmbedIO.Modules
{
    using System;
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// A module that redirects requests.
    /// </summary>
    public class RedirectModule
        : WebModuleBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedirectModule"/> class
        /// that will redirect all served requests.
        /// </summary>
        /// <param name="baseUrlPath">The base URL path.</param>
        /// <param name="redirectUrl">The redirect URL.</param>
        /// <param name="statusCode">The response status code; default is <c>302 - Found</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="redirectUrl"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="redirectUrl"/> is not a valid URL.</para>
        /// <para>- or -</para>
        /// <para><paramref name="statusCode"/> is not a redirection (3xx) status code.</para>
        /// </exception>
        /// <seealso cref="WebModuleBase(string)"/>
        public RedirectModule(string baseUrlPath, string redirectUrl, HttpStatusCode statusCode = HttpStatusCode.Found)
            : this(baseUrlPath, redirectUrl, null, statusCode, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedirectModule"/> class
        /// that will redirect all requests for which the <paramref name="shouldRedirect"/> callback
        /// returns <see langword="true"/>.
        /// </summary>
        /// <param name="baseUrlPath">The base URL path.</param>
        /// <param name="redirectUrl">The redirect URL.</param>
        /// <param name="shouldRedirect">A callback function that returns <see langword="true"/>
        /// if a request must be redirected.</param>
        /// <param name="statusCode">The response status code; default is <c>302 - Found</c>.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="redirectUrl"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="shouldRedirect"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="redirectUrl"/> is not a valid URL.</para>
        /// <para>- or -</para>
        /// <para><paramref name="statusCode"/> is not a redirection (3xx) status code.</para>
        /// </exception>
        /// <seealso cref="WebModuleBase(string)"/>
        public RedirectModule(string baseUrlPath, string redirectUrl, Func<IHttpContext, string, bool> shouldRedirect, HttpStatusCode statusCode = HttpStatusCode.Found)
            : this(baseUrlPath, redirectUrl, shouldRedirect, statusCode, true)
        {
        }

        private RedirectModule(string baseUrlPath, string redirectUrl, Func<IHttpContext, string, bool> shouldRedirect, HttpStatusCode statusCode, bool useCallback)
        {
            RedirectUrl = ValidateUrl(nameof(redirectUrl), redirectUrl);

            var status = (int)statusCode;
            if (status < 300 || status > 399)
                throw new ArgumentException("Status code does not imply a redirection.", nameof(statusCode));

            StatusCode = statusCode;
            var shouldRedirect1 = useCallback ? Extensions.NotNull(nameof(shouldRedirect), shouldRedirect) : null;

            AddHandler(
                baseUrlPath,
                Constants.HttpVerbs.Any,
                (context, ct) =>
                {
                    if (shouldRedirect1 != null && !shouldRedirect1(context, context.RequestPath()))
                        return Task.FromResult(false);

                    context.Redirect(RedirectUrl, (int)StatusCode);
                    return Task.FromResult(true);
                });
        }

        /// <summary>
        /// Gets the redirect URL.
        /// </summary>
        public string RedirectUrl { get; }

        /// <summary>
        /// Gets the response status code.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <inheritdoc />
        public override string Name => nameof(RedirectModule);

        private static string ValidateUrl(
            string argumentName,
            string value,
            UriKind uriKind = UriKind.RelativeOrAbsolute)
        {
            Uri uri;
            try
            {
                uri = new Uri(Extensions.NotNull(argumentName, value), uriKind);
            }
            catch (UriFormatException e)
            {
                throw new ArgumentException("URL is not valid.", argumentName, e);
            }

            return uri.ToString();
        }
    }
}