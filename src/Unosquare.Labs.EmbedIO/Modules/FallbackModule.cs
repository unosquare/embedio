namespace Unosquare.Labs.EmbedIO.Modules
{
    using System;
#if NET46
    using System.Net;
#else
    using Net;
#endif
    
    /// <summary>
    /// Represents a module to fallback any request
    /// </summary>
    /// <seealso cref="Unosquare.Labs.EmbedIO.WebModuleBase" />
    public class FallbackModule : WebModuleBase
    {
        /// <summary>
        /// Gets the name of this module.
        /// </summary>
        public override string Name => nameof(FallbackModule);

        /// <summary>
        /// Gets or sets the fallback action.
        /// </summary>
        public Func<WebServer, HttpListenerContext, bool> FallbackAction { get; }

        /// <summary>
        /// Gets the redirect URL.
        /// </summary>
        public string RedirectUrl { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FallbackModule" /> class.
        /// </summary>
        /// <param name="action">The action.</param>
        public FallbackModule(Func<WebServer, HttpListenerContext, bool> action)
        {
            FallbackAction = action;

            AddHandler(ModuleMap.AnyPath, HttpVerbs.Any, (server, context) =>
            {
                return FallbackAction(server, context);
            });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FallbackModule"/> class.
        /// </summary>
        /// <param name="redirectUrl">The redirect URL.</param>
        public FallbackModule(string redirectUrl)
        {
            RedirectUrl = redirectUrl;

            AddHandler(ModuleMap.AnyPath, HttpVerbs.Any, (server, context) =>
            {
                context.Redirect(redirectUrl, true);
                return true;
            });
        }
    }
}
