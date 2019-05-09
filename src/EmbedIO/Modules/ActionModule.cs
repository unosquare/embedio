using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Constants;

namespace EmbedIO.Modules
{
    /// <summary>
    /// Represents a module to fallback any request.
    /// </summary>
    /// <seealso cref="WebModuleBase" />
    public class ActionModule : WebModuleBase
    {
        private readonly WebHandler _webHandler;
        private readonly HttpVerbs _httpVerb;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionModule" /> class.
        /// </summary>
        /// <param name="baseUrlPath">The base URL path.</param>
        /// <param name="verb">The verb.</param>
        /// <param name="action">The action.</param>
        public ActionModule(string baseUrlPath, HttpVerbs verb, WebHandler action)
            : base(baseUrlPath)
        {
            _webHandler = action;
            _httpVerb = verb;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionModule" /> class.
        /// </summary>
        /// <param name="baseUrlPath">The base URL path.</param>
        /// <param name="redirectUrl">The redirect URL.</param>
        /// <param name="verb">The verb.</param>
        /// <exception cref="ArgumentNullException">redirectUrl.</exception>
        public ActionModule(string baseUrlPath, string redirectUrl, HttpVerbs verb = HttpVerbs.Any)
            : base(baseUrlPath)
        {
            if (string.IsNullOrWhiteSpace(redirectUrl))
                throw new ArgumentNullException(nameof(redirectUrl));

            RedirectUrl = redirectUrl;

            _webHandler = (context, ct) => Task.FromResult(context.Redirect(redirectUrl));
            _httpVerb = verb;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionModule" /> class.
        /// </summary>
        /// <param name="baseUrlPath">The base URL path.</param>
        /// <param name="file">The file.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="verb">The verb.</param>
        /// <exception cref="ArgumentNullException">file.</exception>
        public ActionModule(string baseUrlPath, FileInfo file, string contentType = null,
            HttpVerbs verb = HttpVerbs.Any)
            : base(baseUrlPath)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            _webHandler = (context, ct) => context.FileResponseAsync(file, contentType, true, ct);
            _httpVerb = verb;
        }

        /// <summary>
        /// Gets the redirect URL.
        /// </summary>
        public string RedirectUrl { get; }

        /// <inheritdoc />
        public override Task<bool> HandleRequestAsync(IHttpContext context, string path, CancellationToken ct) =>
            _httpVerb == HttpVerbs.Any || context.RequestVerb() == _httpVerb
                ? _webHandler(context, ct)
                : Task.FromResult(false);
    }
}