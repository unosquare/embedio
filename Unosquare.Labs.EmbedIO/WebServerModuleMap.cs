namespace Unosquare.Labs.EmbedIO
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a 2-key dictionary binding Paths and Verbs to Method calls
    /// </summary>
    public class WebServerModuleMap :
        Dictionary<string, Dictionary<HttpVerbs, WebServerModule.ResponseHandler>>
    {

        /// <summary>
        /// Defines the path used to bind to all paths
        /// </summary>
        public const string AnyPath = "*";

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServerModuleMap"/> class.
        /// </summary>
        public WebServerModuleMap()
            : base(StringComparer.InvariantCultureIgnoreCase)
        {
            // placeholder
        }
    }


}
