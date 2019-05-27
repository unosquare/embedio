using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace EmbedIO.WebApi
{
    /// <summary>
    /// Provides utility methods related to the conversion of route parameters
    /// to parameters of a <see cref="WebApiController"/> handler method.
    /// </summary>
    public static class RouteParameterConverter
    {
        // It doesn't matter which converter we get here: ConvertFromInvariantString is not virtual.
        private static readonly MethodInfo ConvertFromInvariantStringMethod
            = new Func<string, object>(TypeDescriptor.GetConverter(typeof(int)).ConvertFromInvariantString).Method;

        /// <summary>
        /// Determines whether a route parameter can be converted to the specified type.
        /// </summary>
        /// <param name="type">The type to convert to.</param>
        /// <returns><see langword="true"/> if a route parameter can be converted to <paramref name="type"/>;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool CanConvertTo(Type type)
            => type != null
            && TypeDescriptor.GetConverter(type).CanConvertFrom(typeof(string));

        /// <summary>
        /// Converts a string to the specified type.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <param name="type">The type to convert <paramref name="str"/> to.</param>
        /// <returns>The result of the conversion.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">The conversion cannot be performed.</exception>
        public static object Convert(string str, Type type)
            => TypeDescriptor.GetConverter(type).ConvertFromInvariantString(str);

        /// <summary>
        /// Converts a string to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert <paramref name="str"/> to.</typeparam>
        /// <param name="str">The string to convert.</param>
        /// <returns>The result of the conversion.</returns>
        /// <exception cref="NotSupportedException">The conversion cannot be performed.</exception>
        public static T Convert<T>(string str)
            => (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(str);

        internal static Expression ConvertExpression(Expression str, Type type)
        {
            var converter = TypeDescriptor.GetConverter(type);
            return converter.CanConvertFrom(typeof(string))
                ? Expression.Convert(
                    Expression.Call(Expression.Constant(converter), ConvertFromInvariantStringMethod, str),
                    type)
                : null;
        }
    }
}