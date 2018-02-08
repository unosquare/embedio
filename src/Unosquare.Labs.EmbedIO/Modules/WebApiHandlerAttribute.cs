namespace Unosquare.Labs.EmbedIO.Modules
{
    using System;
    using Constants;
    
    /// <summary>
    /// Decorate methods within controllers with this attribute in order to make them callable from the Web API Module
    /// Method Must match the WebServerModule.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class WebApiHandlerAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiHandlerAttribute"/> class.
        /// </summary>
        /// <param name="verb">The verb.</param>
        /// <param name="paths">The paths.</param>
        /// <exception cref="ArgumentException">The argument 'paths' must be specified.</exception>
        public WebApiHandlerAttribute(HttpVerbs verb, string[] paths)
        {
            if (paths == null || paths.Length == 0)
            {
                throw new ArgumentException("The argument 'paths' must be specified.");
            }

            Verb = verb;
            Paths = paths;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiHandlerAttribute"/> class.
        /// </summary>
        /// <param name="verb">The verb.</param>
        /// <param name="path">The path.</param>
        /// <exception cref="ArgumentException">The argument 'path' must be specified.</exception>
        public WebApiHandlerAttribute(HttpVerbs verb, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("The argument 'path' must be specified.");
            }

            Verb = verb;
            Paths = new[] { path };
        }

        /// <summary>
        /// Gets or sets the verb.
        /// </summary>
        /// <value>
        /// The verb.
        /// </value>
        public HttpVerbs Verb { get; protected set; }

        /// <summary>
        /// Gets or sets the paths.
        /// </summary>
        /// <value>
        /// The paths.
        /// </value>
        public string[] Paths { get; protected set; }
    }
}