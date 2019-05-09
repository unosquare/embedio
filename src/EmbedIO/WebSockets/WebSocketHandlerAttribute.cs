using System;

namespace EmbedIO.Modules
{
    /// <summary>
    /// Decorate methods within controllers with this attribute in order to make them callable from the Web API Module
    /// Method Must match the WebServerModule.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class WebSocketHandlerAttribute 
        : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketHandlerAttribute"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <exception cref="System.ArgumentException">The argument 'paths' must be specified.</exception>
        public WebSocketHandlerAttribute(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("The argument 'path' must be specified.");
            }

            Path = path;
        }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>
        /// The paths.
        /// </value>
        public string Path { get; }
    }
}
