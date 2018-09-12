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
        public FallbackModule(Func<IHttpContext, CancellationToken, bool> action)
        {
            AddHandler(
                ModuleMap.AnyPath, 
                HttpVerbs.Any, 
                (context, ct) => Task.FromResult(action(context, ct)));
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="FallbackModule" /> class.
        /// </summary>
        /// <param name="action">The action.</param>
        public FallbackModule(Func<IHttpContext, CancellationToken, Task<bool>> action)
        {
            AddHandler(
                ModuleMap.AnyPath, 
                HttpVerbs.Any, 
                action);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FallbackModule"/> class.
        /// </summary>
        /// <param name="redirectUrl">The redirect URL.</param>
        public FallbackModule(string redirectUrl)
        {
            if (string.IsNullOrWhiteSpace(redirectUrl))
                throw new ArgumentNullException(nameof(redirectUrl));

            RedirectUrl = redirectUrl;

            AddHandler(
                ModuleMap.AnyPath, 
                HttpVerbs.Any, 
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