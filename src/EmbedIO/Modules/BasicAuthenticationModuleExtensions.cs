using System;
using EmbedIO.Utilities;

namespace EmbedIO.Modules
{
    /// <summary>
    /// Provides extension methods for <see cref="BasicAuthenticationModule"/>.
    /// </summary>
    public static class BasicAuthenticationModuleExtensions
    {
        /// <summary>
        /// Adds a username and password to the <see cref="BasicAuthenticationModule.Accounts">Accounts</see> dictionary.
        /// </summary>
        /// <param name="this">The <see cref="BasicAuthenticationModule"/> on which this method is called.</param>
        /// <param name="userName">The user name.</param>
        /// <param name="password">The password.</param>
        /// <returns><paramref name="this"/>, with the user name and password added.</returns>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="this"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="userName"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="OverflowException">
        /// <para>The <see cref="BasicAuthenticationModule.Accounts">Accounts</see> dictionary already contains
        /// the maximum number of elements (<see cref="int.MaxValue">MaxValue</see>).</para>
        /// </exception>
        /// <remarks>
        /// <para>If a <paramref name="userName"/> account already exists,
        /// its password is replaced with <paramref name="password"/>.</para>
        /// </remarks>
        public static BasicAuthenticationModule WithAccount(this BasicAuthenticationModule @this, string userName, string password)
        {
            Validate.NotNull(nameof(@this), @this).Accounts.AddOrUpdate(userName, password, (_, __) => password);

            return @this;
        }
    }
}