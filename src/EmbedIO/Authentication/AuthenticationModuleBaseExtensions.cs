namespace EmbedIO.Authentication
{
    /// <summary>
    /// Provides extension methods for classes derived from <see cref="AuthenticationModuleBase"/>.
    /// </summary>
    public static class AuthenticationModuleBaseExtensions
    {
        /// <summary>
        /// Sets an <see cref="AuthenticationHandlerCallback"/> that is called by an authentication module
        /// when authentication could not take place.
        /// </summary>
        /// <typeparam name="TModule">The type of the module on which this method is called.</typeparam>
        /// <param name="this">The module on which this method is called.</param>
        /// <param name="callback">The method to call.</param>
        /// <returns><paramref name="this"/> with its <see cref="AuthenticationModuleBase.OnMissingCredentials">OnMissingCredentials</see>
        /// property set to <paramref name="callback"/>.</returns>
        public static TModule HandleMissingCredentials<TModule>(this TModule @this, AuthenticationHandlerCallback callback)
            where TModule : AuthenticationModuleBase
        {
            @this.OnMissingCredentials = callback;
            return @this;
        }

        /// <summary>
        /// Sets an <see cref="AuthenticationHandlerCallback"/> that is called by an authentication module
        /// when a request contains invalid credentials.
        /// </summary>
        /// <typeparam name="TModule">The type of the module on which this method is called.</typeparam>
        /// <param name="this">The module on which this method is called.</param>
        /// <param name="callback">The method to call.</param>
        /// <returns><paramref name="this"/> with its <see cref="AuthenticationModuleBase.OnInvalidCredentials">OnInvalidCredentials</see>
        /// property set to <paramref name="callback"/>.</returns>
        public static TModule HandleInvalidCredentials<TModule>(this TModule @this, AuthenticationHandlerCallback callback)
            where TModule : AuthenticationModuleBase
        {
            @this.OnInvalidCredentials = callback;
            return @this;
        }
    }
}