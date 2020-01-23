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
    }
}