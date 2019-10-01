using System;
using System.Net;
using System.Threading.Tasks;
using EmbedIO.Utilities;

namespace EmbedIO.Actions
{
    /// <summary>
    /// A module that redirects requests.
    /// </summary>
    /// <seealso cref="WebModuleBase" />
    public class RedirectModule : WebModuleBase
    {
        private readonly Func<IHttpContext, bool>? _shouldRedirect;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedirectModule"/> class
        /// that will redirect all served requests.
        /// </summary>
        /// <param name="baseRoute">The base route.</param>
        /// <param name="redirectUrl">The redirect URL.</param>
        /// <param name="statusCode">The response status code; default is <c>302 - Found</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="redirectUrl"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="redirectUrl"/> is not a valid URL.</para>
        /// <para>- or -</para>
        /// <para><paramref name="statusCode"/> is not a redirection (3xx) status code.</para>
        /// </exception>
        /// <seealso cref="WebModuleBase(string)"/>
        public RedirectModule(string baseRoute, string redirectUrl, HttpStatusCode statusCode = HttpStatusCode.Found)
            : this(baseRoute, redirectUrl, null, statusCode, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedirectModule"/> class
        /// that will redirect all requests for which the <paramref name="shouldRedirect"/> callback
        /// returns <see langword="true"/>.
        /// </summary>
        /// <param name="baseRoute">The base route.</param>
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
        public RedirectModule(string baseRoute, string redirectUrl, Func<IHttpContext, bool>? shouldRedirect, HttpStatusCode statusCode = HttpStatusCode.Found)
            : this(baseRoute, redirectUrl, shouldRedirect, statusCode, true)
        {
        }

        private RedirectModule(string baseRoute, string redirectUrl, Func<IHttpContext, bool>? shouldRedirect, HttpStatusCode statusCode, bool useCallback)
            : base(baseRoute)
        {
            RedirectUrl = Validate.Url(nameof(redirectUrl), redirectUrl);

            var status = (int)statusCode;
            if (status < 300 || status > 399)
                throw new ArgumentException("Status code does not imply a redirection.", nameof(statusCode));

            StatusCode = statusCode;
            _shouldRedirect = useCallback ? Validate.NotNull(nameof(shouldRedirect), shouldRedirect) : null;
        }

        /// <inheritdoc />
        public override bool IsFinalHandler => false;

        /// <summary>
        /// Gets the redirect URL.
        /// </summary>
        public string RedirectUrl { get; }

        /// <summary>
        /// Gets the response status code.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <inheritdoc />
        protected override Task OnRequestAsync(IHttpContext context)
        {
            if (_shouldRedirect?.Invoke(context) ?? true)
            {
                context.Redirect(RedirectUrl, (int)StatusCode);
                context.SetHandled();
            }

            return Task.CompletedTask;
        }
    }
}