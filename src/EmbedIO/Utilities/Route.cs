using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using EmbedIO.Modules;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// Provides utility methods to work with routes.
    /// </summary>
    /// <seealso cref="WebApiModule"/>
    /// <seealso cref="WebApiController"/>
    /// <seealso cref="RouteHandlerAttribute"/>
    public static class Route
    {
        // Characters in ValidParameterNameChars MUST be in ascending ordinal order!
        private static readonly char[] ValidParameterNameChars =
            "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz".ToCharArray();

        /// <summary>
        /// <para>Determines whether a string is a valid route parameter name.</para>
        /// <para>To be considered a valid route parameter name, the specified string:</para>
        /// <list type="bullet">
        /// <item><description>must not be <see langword="null"/>;</description></item>
        /// <item><description>must not be the empty string;</description></item>
        /// <item><description>must consist entirely of decimal digits, upper- or lower-case
        /// letters of the English alphabet, or underscore (<c>'_'</c>) characters;</description></item>
        /// <item><description>must not start with a decimal digit.</description></item>
        /// </list>
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is a valid route parameter;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool IsValidParameterName(string value)
            => !string.IsNullOrEmpty(value)
            && value[0] > '9'
            && !value.Any(c => c < '0' || c > 'z' || Array.BinarySearch(ValidParameterNameChars, c) < 0);

        /// <summary>
        /// <para>Determines whether a string is a valid route.</para>
        /// <para>To be considered a valid route, the specified string:</para>
        /// <list type="bullet">
        /// <item><description>must not be <see langword="null"/>;</description></item>
        /// <item><description>must not be the empty string;</description></item>
        /// <item><description>must start with a slash (<c>'/'</c>) character;</description></item>
        /// <item><description>must not end with a slash (<c>'/'</c>) character,
        /// unless it is the only character in the string;</description></item>
        /// <item><description>may contain one or more parameter specifications.</description></item>
        /// <para>Each parameter specification must be enclosed in curly brackets (<c>'{'</c>
        /// and <c>'}'</c>. No whitespace is allowed inside a parameter specification.</para>
        /// <para>A parameter specification consists of a valid parameter name, optionally
        /// followed by a <c>'?'</c> character to signify that it matches an empty string,
        /// or a <c>'!'</c> character if it does not.</para>
        /// <para>If neither <c>'?'</c> nor <c>'!'</c> are present, a parameter by default
        /// matches an empty string.</para>
        /// <para>See <see cref="IsValidParameterName"/> for the definition of a valid parameter name.</para>
        /// <para>To include a literal open curly bracket in the route, it must be doubled (<c>"{{"</c>).</para>
        /// <para>A literal closed curly bracket (<c>'}'</c>) may be included in the route as-is.</para>
        /// </list>
        /// </summary>
        /// <param name="route">The route.</param>
        /// <returns><see langword="true"/> if <paramref name="route"/> is a valid route;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool IsValid(string route) => ValidateInternal(nameof(route), route) == null;

        // Check the validity of a route by parsing it without storing the results.
        // Returns: ArgumentNullException, ArgumentException, null if OK
        internal static Exception ValidateInternal(string argumentName, string value)
        {
            switch (ParseInternal(value, null, null))
            {
                case ArgumentNullException _:
                    return new ArgumentNullException(argumentName);

                case FormatException formatException:
                    return new ArgumentException(formatException.Message, argumentName);

                case Exception exception:
                    return exception;

                default:
                    return null; // Unreachable, but the compiler doesn't know.
            }
        }

        // Validate and parse a route, constructing a Regex pattern.
        // addParameter will be called for each parameter found.
        // setPattern will be called at the end with the constructed pattern.
        // Either callback can be null; is setPattern is null, no pattern is built.
        // Returns: ArgumentNullException, FormatException, null if OK
        internal static Exception ParseInternal(string route, Action<string> addParameter, Action<string> setPattern)
        {
            if (route == null)
                return new ArgumentNullException(nameof(route));

            if (route.Length == 0)
                return new FormatException("Route is empty.");

            if (route[0] != '/')
                return new FormatException("Route does not start with a slash.");

            if (route.Length > 1 && route[route.Length - 1] == '/')
                return new FormatException("Route must not end with a slash unless it is \"/\".");

            /*
             * Regex options set at start of pattern:
             * IgnoreCase              : no
             * Multiline               : no
             * Singleline              : yes
             * ExplicitCapture         : yes
             * IgnorePatternWhitespace : no
             * See https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-options
             * See https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions#group_options
             */

            // If setPattern is null we don't need the StringBuilder.
            var sb = setPattern == null ? null : new StringBuilder("(?sn-imx)^");

            // Parse the string, looking alternately for a '{', that opens a parameter specification,
            // then for a '}', that closes it.
            // Characters outside parameter specifications are Regex-escaped and added to the pattern.
            // A parameter specification consists of a parameter name optionally followed by '?' or '!'
            // to indicate that an empty parameter matches or not, respectively. The default is to match.
            var inParameterSpec = false;
            for (var position = 0; ;)
            {
                if (inParameterSpec)
                {
                    // Look for end of spec, bail out if not found.
                    var closePosition = route.IndexOf('}', position);
                    if (closePosition < 0)
                        return new FormatException("Route syntax error: unclosed parameter specification.");

                    // Parameter spec cannot be empty.
                    if (closePosition == position)
                        return new FormatException("Route syntax error: empty parameter specification.");

                    // Check the last character:
                    // {name} or {name?} means empty parameter matches
                    // {name!} means empty parameter does not match
                    // If '?' or '!' is found, the parameter name ends before it
                    var nameEndPosition = closePosition;
                    bool allowEmpty;
                    switch (route[closePosition - 1])
                    {
                        case '!':
                            allowEmpty = false;
                            nameEndPosition--;
                            break;
                        case '?':
                            allowEmpty = true;
                            nameEndPosition--;
                            break;
                        default:
                            allowEmpty = true;
                            break;
                    }

                    // Bail out if only '?' or '!' is found inside the spec.
                    if (nameEndPosition == position)
                        return new FormatException("Route syntax error: missing parameter name.");

                    // Extract and check the parameter name.
                    var parameterName = route.Substring(position, nameEndPosition - position);
                    if (!IsValidParameterName(parameterName))
                        return new FormatException("Route syntax error: parameter name contains one or more invalid characters.");

                    // The spec is valid, so add the parameter (if requested),
                    // append a capturing group with the same name to the pattern,
                    // and go on with parsing.
                    addParameter?.Invoke(parameterName);
                    sb?.Append("(?<").Append(parameterName).Append(">.").Append(allowEmpty ? '*' : '+').Append(')');
                    position = closePosition + 1;
                    inParameterSpec = false;
                }
                else
                {
                    // Look for start of parameter spec.
                    var openPosition = route.IndexOf('{', position);
                    if (openPosition < 0)
                    {
                        // No more parameter specs: escape the remainder of the string
                        // and add it to the pattern.
                        sb?.Append(Regex.Escape(route.Substring(position)));
                        break;
                    }

                    // If another identical char follows, treat the two as a single literal char.
                    var nextPosition = openPosition + 1;
                    if (nextPosition < route.Length && route[nextPosition] == '{')
                    {
                        sb?.Append(@"\\{");
                    }
                    else
                    {
                        // Escape the part of the pattern outside the parameter spec
                        // and add it to the pattern, then go on parsing.
                        sb?.Append(Regex.Escape(route.Substring(position, openPosition - position)));
                        inParameterSpec = true;
                        position = nextPosition;
                    }
                }
            }

            // Close the pattern and invoke setPattern (if requested).
            setPattern?.Invoke(sb.Append('$').ToString());

            // Everything's fine, thus no exception.
            return null;
        }
    }
}