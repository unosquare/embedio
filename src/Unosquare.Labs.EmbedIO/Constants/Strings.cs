namespace Unosquare.Labs.EmbedIO.Constants
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Defines assembly-wide constants
    /// </summary>
    public static class Strings
    {
        /// <summary>
        ///  Default Browser time format
        /// </summary>
        public const string BrowserTimeFormat = "ddd, dd MMM yyyy HH:mm:ss 'GMT'";

        /// <summary>
        /// Default CORS rule
        /// </summary>
        public const string CorsWildcard = "*";

        /// <summary>
        /// The comma split character for String.Split method calls.
        /// </summary>
        public static readonly char[] CommaSplitChar = { ',' };

        /// <summary>
        /// The format culture used for header outputs
        /// </summary>
        public static readonly CultureInfo StandardCultureInfo =
#if NETFX
            CultureInfo.CreateSpecificCulture("en-US");
#else
            new CultureInfo("en-US");
#endif

        /// <summary>
        /// The standard string comparer
        /// </summary>
        public static StringComparer StandardStringComparer =
#if NETFX
            StringComparer.InvariantCultureIgnoreCase;
#else
           StringComparer.OrdinalIgnoreCase;
#endif

        /// <summary>
        /// The static file string comparer
        /// </summary>
        public static StringComparer StaticFileStringComparer =
#if NETFX
            StringComparer.InvariantCulture;
#else
           StringComparer.Ordinal;
#endif
    }
}
