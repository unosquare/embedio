using System;

namespace EmbedIO.Utilities
{
    partial class Validate
    {
        /// <summary>
        /// Ensures that the value of an argument is a valid route.
        /// </summary>
        /// <param name="argumentName">The name of the argument to validate.</param>
        /// <param name="value">The value to validate.</param>
        /// <param name="isBaseRoute"><see langword="true"/> if the argument must be a base route;
        /// <see langword="false"/> if the argument must be a non-base route.</param>
        /// <returns><paramref name="value"/>, if it is a valid route.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="value"/> is empty.</para>
        /// <para>- or -</para>
        /// <para><paramref name="value"/> does not start with a slash (<c>/</c>) character.</para>
        /// <para>- or -</para>
        /// <para><paramref name="value"/> does not comply with route syntax.</para>
        /// </exception>
        /// <seealso cref="Routing.Route.IsValid"/>
        public static string Route(string argumentName, string value, bool isBaseRoute)
        {
            var exception = Routing.Route.ValidateInternal(argumentName, value, isBaseRoute);
            if (exception != null)
                throw exception;

            return Utilities.UrlPath.UnsafeNormalize(value, isBaseRoute);
        }
    }
}