namespace Unosquare.Labs.EmbedIO
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a 2-key dictionary binding Paths and Verbs to Method calls
    /// </summary>
    public class ModuleMap :
        Dictionary<string, Dictionary<HttpVerbs, ResponseHandler>>
    {

        /// <summary>
        /// Defines the path used to bind to all paths
        /// </summary>
        public const string AnyPath = "*";

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleMap"/> class.
        /// </summary>
        public ModuleMap()
            : base(StringComparer.InvariantCultureIgnoreCase)
        {
            // placeholder
        }
    }


}
