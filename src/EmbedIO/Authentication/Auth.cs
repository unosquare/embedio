using System.Security.Principal;

namespace EmbedIO.Authentication
{
    /// <summary>
    /// Provides useful authentication-related constants.
    /// </summary>
    public static class Auth
    {
        /// <summary>
        /// Gets an <see cref="IPrincipal"/> interface representing
        /// no user. To be used instead of <see langword="null"/>
        /// to initialize or set properties of type <see cref="IPrincipal"/>.
        /// </summary>
        public static IPrincipal NoUser { get; } = new GenericPrincipal(
            new GenericIdentity(string.Empty, string.Empty),
            null);

        /// <summary>
        /// Creates and returns an <see cref="IPrincipal"/> interface
        /// representing an unauthenticated user, with the given
        /// authentication type.
        /// </summary>
        /// <param name="authenticationType">The type of authentication used to identify the user.</param>
        /// <returns>An <see cref="IPrincipal"/> interface.</returns>
        public static IPrincipal CreateUnauthenticatedPrincipal(string authenticationType)
            => new GenericPrincipal(
                new GenericIdentity(string.Empty, authenticationType),
                null);
    }
}