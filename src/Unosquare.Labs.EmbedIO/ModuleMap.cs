﻿namespace Unosquare.Labs.EmbedIO
{
    using Constants;
    using System.Collections.Generic;

    /// <inheritdoc />
    /// <summary>
    /// Represents a list which binds Paths and their corresponding HTTP Verbs to Method calls.
    /// </summary>
    public class ModuleMap : List<Map>
    {
        /// <summary>
        /// Defines the path used to bind to all paths.
        /// </summary>
        public const string AnyPath = "*";

        internal const string AnyPathRoute = "/*";
    }

    /// <summary>
    /// Represents a binding of path and verb to a given method call (delegate).
    /// </summary>
    public class Map
    {
        /// <summary>
        /// The HTTP resource path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The HTTP Verb of this Map.
        /// </summary>
        public HttpVerbs Verb { get; set; }

        /// <summary>
        /// The delegate to call for the given path and verb.
        /// </summary>
        public WebHandler ResponseHandler { get; set; }
    }
}