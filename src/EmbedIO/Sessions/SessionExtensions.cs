using System;

namespace EmbedIO.Sessions
{
    /// <summary>
    /// Provides extension methods for types implementing <see cref="ISession"/>.
    /// </summary>
    public static class SessionExtensions
    {
        /// <summary>Gets the value associated with the specified key.</summary>
        /// <typeparam name="T">The desired type of the value.</typeparam>
        /// <param name="this">The <see cref="ISession"/> on which this method is called.</param>
        /// <param name="key">The key whose value to get from the session.</param>
        /// <param name="value">
        /// <para>When this method returns, the value associated with the specified key,
        /// if the key is found and the associated value is of type <typeparamref name="T"/>;
        /// otherwise, the default value for <typeparamref name="T"/>.</para>
        /// <para>This parameter is passed uninitialized.</para>
        /// </param>
        /// <returns><see langword="true"/> if the key is found and the associated value is of type <typeparamref name="T"/>;
        /// otherwise, <see langword="false"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        public static bool TryGetValue<T>(this ISession @this, string key, out T value)
        {
            if (@this.TryGetValue(key, out var foundValue) && foundValue is T typedValue)
            {
                value = typedValue;
                return true;
            }

#pragma warning disable CS8653 // "default" can be null - We are returning false, so value is undefined
            value = default;
#pragma warning restore CS8653
            return false;
        }

        /// <summary>Gets the value associated with the specified key.</summary>
        /// <typeparam name="T">The desired type of the value.</typeparam>
        /// <param name="this">The <see cref="ISession"/> on which this method is called.</param>
        /// <param name="key">The key whose value to get from the session.</param>
        /// <returns>The value associated with the specified key,
        /// if the key is found and the associated value is of type <typeparamref name="T"/>;
        /// otherwise, the default value for <typeparamref name="T"/>.</returns>
        public static T GetValue<T>(this ISession @this, string key)
            => @this.TryGetValue(key, out var value) && value is T typedValue ? typedValue : default;

        /// <summary>Gets the value associated with the specified key.</summary>
        /// <typeparam name="T">The desired type of the value.</typeparam>
        /// <param name="this">The <see cref="ISession"/> on which this method is called.</param>
        /// <param name="key">The key whose value to get from the session.</param>
        /// <param name="defaultValue">The default value to return if the key is not found
        /// or its associated value is not of type <typeparamref name="T"/>.</param>
        /// <returns>The value associated with the specified key,
        /// if the key is found and the associated value is of type <typeparamref name="T"/>;
        /// otherwise, <paramref name="defaultValue"/>.</returns>
        public static T GetOrDefault<T>(this ISession @this, string key, T defaultValue)
            => @this.TryGetValue(key, out var value) && value is T typedValue ? typedValue : defaultValue;
    }
}