﻿namespace Unosquare.Labs.EmbedIO.Modules
{
    using Constants;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
#if NET47
    using System.Net;
#else
    using Net;
#endif

    /// <summary>
    /// Represents a module to fallback any request
    /// </summary>
    /// <seealso cref="WebModuleBase" />
    public class FallbackModule 
        : WebModuleBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FallbackModule" /> class.
        /// </summary>
        /// <param name="action">The action.</param>
        public FallbackModule(Func<HttpListenerContext, CancellationToken, bool> action)
        {
            AddHandler(
                ModuleMap.AnyPath, 
                HttpVerbs.Any, 
                (context, ct) => Task.FromResult(action(context, ct)));
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

        /// <summary>
        /// Gets the name of this module.
        /// </summary>
        public override string Name => nameof(FallbackModule);

        /// <summary>
        /// Gets the redirect URL.
        /// </summary>
        public string RedirectUrl { get; }
    }
}