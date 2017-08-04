﻿namespace Unosquare.Labs.EmbedIO.Constants
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Defines assembly-wide constants
    /// </summary>
    internal static class Strings
    {
        /// <summary>
        ///  Default Browser time format
        /// </summary>
        internal const string BrowserTimeFormat = "ddd, dd MMM yyyy HH:mm:ss 'GMT'";

        /// <summary>
        /// Default CORS rule
        /// </summary>
        internal const string CorsWildcard = "*";

        /// <summary>
        /// The comma split character for String.Split method calls.
        /// </summary>
        internal static readonly char[] CommaSplitChar = { ',' };

        /// <summary>
        /// The format culture used for header outputs
        /// </summary>
        internal static CultureInfo StandardCultureInfo { get; } =
#if NETFX
            CultureInfo.CreateSpecificCulture("en-US");
#else
            new CultureInfo("en-US");
#endif

        /// <summary>
        /// The standard string comparer
        /// </summary>
        internal static StringComparer StandardStringComparer { get; } =
#if NETFX
            StringComparer.InvariantCultureIgnoreCase;
#else
           StringComparer.OrdinalIgnoreCase;
#endif

        /// <summary>
        /// The static file string comparer
        /// </summary>
        internal static StringComparer StaticFileStringComparer { get; } =
#if NETFX
            StringComparer.InvariantCulture;
#else
           StringComparer.Ordinal;
#endif
    }
}
