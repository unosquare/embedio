using System;
using System.Collections.Generic;
using EmbedIO.Utilities;

namespace EmbedIO.Authentication
{
    /// <summary>
    /// Provides extension methods for <see cref="BasicAuthenticationModule"/>.
    /// </summary>
    public static class BasicAuthenticationModuleExtensions
    {
        /// <summary>
        /// Adds a user name and password to the <see cref="BasicAuthenticationModule.Accounts">Accounts</see> dictionary
        /// of a <see cref="BasicAuthenticationModule"/>.
        /// </summary>
        /// <param name="this">The <see cref="BasicAuthenticationModule"/> on which this method is called.</param>
        /// <param name="userName">The user name.</param>
        /// <param name="password">The password.</param>
        /// <returns><paramref name="this"/>, with the user name and password added to the
        /// <see cref="BasicAuthenticationModule.Accounts">Accounts</see> dictionary.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="userName"/> is <see langword="null"/>.</exception>
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
            @this.Accounts.AddOrUpdate(userName, password, (_, __) => password);
            return @this;
        }

        /// <summary>
        /// Adds a dictionary of user names and passwords to the <see cref="BasicAuthenticationModule.Accounts">Accounts</see> dictionary
        /// of a <see cref="BasicAuthenticationModule"/>.
        /// </summary>
        /// <param name="this">The <see cref="BasicAuthenticationModule"/> on which this method is called.</param>
        /// <param name="accounts">An enumeration of key / value pairs
        /// representing the user names and passwords to add..</param>
        /// <returns><paramref name="this"/>, with the user names and passwords in <paramref name="accounts"/>
        /// added to the <see cref="BasicAuthenticationModule.Accounts">Accounts</see> dictionary.</returns>
        /// <exception cref="NullReferenceException">
        /// <para><paramref name="this"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="accounts"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="ArgumentNullException">One or more of the keys in <paramref name="accounts"/> are <see langword="null"/>.</exception>
        /// <exception cref="OverflowException">
        /// <para>The <see cref="BasicAuthenticationModule.Accounts">Accounts</see> dictionary already contains
        /// the maximum number of elements (<see cref="int.MaxValue">MaxValue</see>).</para>
        /// </exception>
        public static BasicAuthenticationModule WithAccounts(this BasicAuthenticationModule @this, [ValidatedNotNull] IEnumerable<KeyValuePair<string, string>> accounts)
        {
            foreach (var account in accounts)
            {
                @this.Accounts.AddOrUpdate(account.Key, account.Value, (_, __) => account.Value);
            }

            return @this;
        }
    }
}