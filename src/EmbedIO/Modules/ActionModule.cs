using System;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Constants;
using EmbedIO.Utilities;

namespace EmbedIO.Modules
{
    /// <summary>
    /// A module that passes requests to a callback.
    /// </summary>
    /// <seealso cref="WebModuleBase" />
    public class ActionModule : WebModuleBase
    {
        private readonly HttpVerbs _verb;

        private readonly WebHandler _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionModule" /> class.
        /// </summary>
        /// <param name="baseUrlPath">The base URL path.</param>
        /// <param name="verb">The HTTP verb that will be served by this module.</param>
        /// <param name="handler">The callback used to handle requests.</param>
        /// <exception cref="ArgumentNullException"><paramref name="handler"/> is <see langword="null"/>.</exception>
        /// <seealso cref="WebModuleBase(string)"/>
        public ActionModule(string baseUrlPath, HttpVerbs verb, WebHandler handler)
            : base(baseUrlPath)
        {
            _verb = verb;
            _handler = Validate.NotNull(nameof(handler), handler);
        }

        /// <inheritdoc />
        public override async Task<bool> HandleRequestAsync(IHttpContext context, string path, CancellationToken ct) =>
            (_verb == HttpVerbs.Any || context.RequestVerb() == _verb)
            && await _handler(context, path, ct).ConfigureAwait(false);
    }
}