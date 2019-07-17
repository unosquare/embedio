using System;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Utilities;

namespace EmbedIO.Actions
{
    /// <summary>
    /// A module that passes requests to a callback.
    /// </summary>
    /// <seealso cref="WebModuleBase" />
    public class ActionModule : WebModuleBase
    {
        private readonly HttpVerbs _verb;

        private readonly RequestHandlerCallback _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionModule" /> class.
        /// </summary>
        /// <param name="baseUrlPath">The base URL path.</param>
        /// <param name="verb">The HTTP verb that will be served by this module.</param>
        /// <param name="handler">The callback used to handle requests.</param>
        /// <exception cref="ArgumentNullException"><paramref name="handler"/> is <see langword="null"/>.</exception>
        /// <seealso cref="WebModuleBase(string)"/>
        public ActionModule(string baseUrlPath, HttpVerbs verb, RequestHandlerCallback handler)
            : base(baseUrlPath)
        {
            _verb = verb;
            _handler = Validate.NotNull(nameof(handler), handler);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionModule"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        public ActionModule(RequestHandlerCallback handler)
            : this("/", HttpVerbs.Any, handler)
        {
        }

        /// <inheritdoc />
        public override bool IsFinalHandler => false;

        /// <inheritdoc />
        protected override async Task OnRequestAsync(IHttpContext context, string path, CancellationToken cancellationToken)
        {
            if (_verb != HttpVerbs.Any && context.Request.HttpVerb != _verb)
                return;

            await _handler(context, path, cancellationToken).ConfigureAwait(false);
            context.Handled = true;
        }
    }
}