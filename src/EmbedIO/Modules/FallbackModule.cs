using System;
using System.IO;
using System.Threading.Tasks;
using EmbedIO.Constants;

namespace EmbedIO.Modules
{
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

        /// <summary>
        /// Initializes a new instance of the <see cref="FallbackModule" /> class.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="verb">The verb.</param>
        /// <exception cref="ArgumentNullException">file.</exception>
        public FallbackModule(FileInfo file, string contentType = null, HttpVerbs verb = HttpVerbs.Any)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            AddHandler(
                ModuleMap.AnyPath, 
                verb, 
                (context, ct) => context.FileResponseAsync(file, contentType, true, ct));
        }

        /// <inheritdoc />
        public override string Name => nameof(FallbackModule);

        /// <summary>
        /// Gets the redirect URL.
        /// </summary>
        public string RedirectUrl { get; }
    }
}