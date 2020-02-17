using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO.Authentication
{
    /// <summary>
    /// Simple HTTP basic authentication module that stores user names and passwords
    /// in a <seealso cref="ConcurrentDictionary{TKey,TValue}"/>, and has no user roles.
    /// </summary>
    public class BasicAuthenticationModule : BasicAuthenticationModuleBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BasicAuthenticationModule"/> class.
        /// </summary>
        /// <param name="baseRoute">The base route.</param>
        /// <param name="realm">The authentication realm.</param>
        /// <remarks>
        /// <para>If <paramref name="realm"/> is <see langword="null"/> or the empty string,
        /// the <see cref="BasicAuthenticationModuleBase.Realm">Realm</see> property will be set equal to
        /// <see cref="IWebModule.BaseRoute">BaseRoute</see>.</para>
        /// </remarks>
        public BasicAuthenticationModule(string baseRoute, string? realm = null)
            : base(baseRoute, realm)
        {
        }

        /// <summary>
        /// Gets a dictionary of valid user names and passwords.
        /// </summary>
        public ConcurrentDictionary<string, string> Accounts { get; } = new ConcurrentDictionary<string, string>(StringComparer.InvariantCulture);

        /// <inheritdoc />
        protected sealed override Task<bool> VerifyCredentialsAsync(string path, string userName, string password, IList<string> roles, CancellationToken cancellationToken)
            => Task.FromResult(VerifyCredentialsInternal(userName, password));

        private bool VerifyCredentialsInternal(string userName, string password)
            => Accounts.TryGetValue(userName, out var storedPassword)
            && string.Equals(password, storedPassword, StringComparison.Ordinal);
    }
}