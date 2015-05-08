namespace Unosquare.Labs.EmbedIO
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Map object
    /// </summary>
    public class Map
    {
        /// <summary>
        /// Route path
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// HTTP Verb
        /// </summary>
        public HttpVerbs Verb { get; set; }
        /// <summary>
        /// ResponseHandler method
        /// </summary>
        public ResponseHandler ResponseHandler { get; set; }

    }
    /// <summary>
    /// Represents a list binding Paths and Verbs to Method calls
    /// </summary>
    public class ModuleMap : List<Map>
    {

        /// <summary>
        /// Defines the path used to bind to all paths
        /// </summary>
        public const string AnyPath = "*";
    }
}