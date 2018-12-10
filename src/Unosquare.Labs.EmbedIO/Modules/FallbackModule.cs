namespace Unosquare.Labs.EmbedIO.Modules
{
    using Constants;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a module to fallback any request.
    /// </summary>
    /// <seealso cref="WebModuleBase" />
    public class FallbackModule 
        : WebModuleBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FallbackModule" /> class.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="verb">The verb.</param>
        public FallbackModule(Func<IHttpContext, CancellationToken, bool> action, HttpVerbs verb = HttpVerbs.Any)
        {
            AddHandler(
                ModuleMap.AnyPath, 
                verb, 
                (context, ct) => Task.FromResult(action(context, ct)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FallbackModule" /> class.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="verb">The verb.</param>
        public FallbackModule(WebHandler action, HttpVerbs verb = HttpVerbs.Any)
        {
            AddHandler(
                ModuleMap.AnyPath, 
                verb,
                action);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FallbackModule" /> class.
        /// </summary>
        /// <param name="redirectUrl">The redirect URL.</param>
        /// <param name="verb">The verb.</param>
        /// <exception cref="ArgumentNullException">redirectUrl.</exception>
        public FallbackModule(string redirectUrl, HttpVerbs verb = HttpVerbs.Any)
        {
            if (string.IsNullOrWhiteSpace(redirectUrl))
                throw new ArgumentNullException(nameof(redirectUrl));

            RedirectUrl = redirectUrl;

            AddHandler(
                ModuleMap.AnyPath, 
                verb, 
                (context, ct) => Task.FromResult(context.Redirect(redirectUrl)));
        }

        /// <inheritdoc />
        public override string Name => nameof(FallbackModule);

        /// <summary>
        /// Gets the redirect URL.
        /// </summary>
        public string RedirectUrl { get; }
    }
}