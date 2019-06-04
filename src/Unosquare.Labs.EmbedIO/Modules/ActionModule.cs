namespace Unosquare.Labs.EmbedIO.Modules
{
    using System;
    using Constants;

    /// <summary>
    /// A module that passes requests to a callback.
    /// </summary>
    public class ActionModule
        : WebModuleBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActionModule" /> class.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="verb">The HTTP verb that will be served by this module.</param>
        /// <param name="handler">The callback used to handle requests.</param>
        /// <exception cref="ArgumentNullException"><paramref name="handler" /> is <see langword="null" />.</exception>
        public ActionModule(string url, HttpVerbs verb, WebHandler handler)
        {
            AddHandler(url, verb, handler);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionModule"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        public ActionModule(WebHandler handler)
            : this(ModuleMap.AnyPath, HttpVerbs.Any, handler) { }

        /// <inheritdoc />
        public override string Name => nameof(ActionModule);
    }
}
