using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// Provides a standard way to convert strings to different types.
    /// </summary>
    public static class FromString
    {
        // It doesn't matter which converter we get here: ConvertFromInvariantString is not virtual.
        private static readonly MethodInfo ConvertFromInvariantStringMethod
            = new Func<string, object>(TypeDescriptor.GetConverter(typeof(int)).ConvertFromInvariantString).Method;

        private static readonly MethodInfo TryConvertToInternalMethod
            = typeof(FromString).GetMethod(nameof(TryConvertToInternal), BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly MethodInfo ConvertToInternalMethod
            = typeof(FromString).GetMethod(nameof(ConvertToInternal), BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly ConcurrentDictionary<Type, Func<string[], (bool Success, object Result)>> GenericTryConvertToMethods
            = new ConcurrentDictionary<Type, Func<string[], (bool Success, object Result)>>();

        private static readonly ConcurrentDictionary<Type, Func<string[], object>> GenericConvertToMethods
            = new ConcurrentDictionary<Type, Func<string[], object>>();

        /// <summary>
        /// Determines whether a string can be converted to the specified type.
        /// </summary>
        /// <param name="type">The type resulting from the conversion.</param>
        /// <returns><see langword="true" /> if the conversion is possible;
        /// otherwise, <see langword="false" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type" /> is <see langword="null" />.</exception>
        public static bool CanConvertTo(Type type)
            => TypeDescriptor.GetConverter(type).CanConvertFrom(typeof(string));

        /// <summary>
        /// Determines whether a string can be converted to the specified type.
        /// </summary>
        /// <typeparam name="TResult">The type resulting from the conversion.</typeparam>
        /// <returns><see langword="true" /> if the conversion is possible;
        /// otherwise, <see langword="false" />.</returns>
        public static bool CanConvertTo<TResult>()
            => TypeDescriptor.GetConverter(typeof(TResult)).CanConvertFrom(typeof(string));

        /// <summary>
        /// Attempts to convert a string to the specified type.
        /// </summary>
        /// <param name="type">The type resulting from the conversion.</param>
        /// <param name="str">The string to convert.</param>
        /// <param name="result">When this method returns <see langword="true" />,
        /// the result of the conversion. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true" /> if the conversion is successful;
        /// otherwise, <see langword="false" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type" /> is <see langword="null" />.</exception>
        public static bool TryConvertTo(Type type, string str, out object result)
        {
            var converter = TypeDescriptor.GetConverter(type);
            if (!converter.CanConvertFrom(typeof(string)))
            {
                result = null;
                return false;
            }

            try
            {
                result = converter.ConvertFromInvariantString(str);
                return true;
            }
            catch (Exception e) when (!e.IsCriticalException())
            {
                result = null;
                return false;
            }
        }

        /// <summary>
        /// Attempts to convert a string to the specified type.
        /// </summary>
        /// <typeparam name="TResult">The type resulting from the conversion.</typeparam>
        /// <param name="str">The string to convert.</param>
        /// <param name="result">When this method returns <see langword="true" />,
        /// the result of the conversion. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true" /> if the conversion is successful;
        /// otherwise, <see langword="false" />.</returns>
        public static bool TryConvertTo<TResult>(string str, out TResult result)
        {
            var converter = TypeDescriptor.GetConverter(typeof(TResult));
            if (!converter.CanConvertFrom(typeof(string)))
            {
                result = default;
                return false;
            }

            try
            {
                result = (TResult)converter.ConvertFromInvariantString(str);
                return true;
            }
            catch (Exception e) when (!e.IsCriticalException())
            {
                result = default;
                return false;
            }
        }

        /// <summary>
        /// Converts a string to the specified type.
        /// </summary>
        /// <param name="type">The type resulting from the conversion.</param>
        /// <param name="str">The string to convert.</param>
        /// <returns>An instance of <paramref name="type" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type" /> is <see langword="null" />.</exception>
        /// <exception cref="StringConversionException">The conversion was not successful.</exception>
        public static object ConvertTo(Type type, string str)
        {
            Validate.NotNull(nameof(type), type);
            try
            {
                return TypeDescriptor.GetConverter(type).ConvertFromInvariantString(str);
            }
            catch (Exception e) when (!e.IsCriticalException())
            {
                throw new StringConversionException(type, e);
            }
        }

        /// <summary>
        /// Converts a string to the specified type.
        /// </summary>
        /// <typeparam name="TResult">The type resulting from the conversion.</typeparam>
        /// <param name="str">The string to convert.</param>
        /// <returns>An instance of <typeparamref name="TResult" />.</returns>
        /// <exception cref="StringConversionException">
        /// The conversion was not successful.
        /// </exception>
        public static TResult ConvertTo<TResult>(string str)
        {
            try
            {
                return (TResult)TypeDescriptor.GetConverter(typeof(TResult)).ConvertFromInvariantString(str);
            }
            catch (Exception e) when (!e.IsCriticalException())
            {
                throw new StringConversionException(typeof(TResult), e);
            }
        }

        /// <summary>
        /// Attempts to convert an array of strings to an array of the specified type.
        /// </summary>
        /// <param name="type">The type resulting from the conversion of each
        /// element of <paramref name="strings"/>.</param>
        /// <param name="strings">The array to convert.</param>
        /// <param name="result">When this method returns <see langword="true" />,
        /// the result of the conversion. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true" /> if the conversion is successful;
        /// otherwise, <see langword="false" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type" /> is <see langword="null" />.</exception>
        public static bool TryConvertTo(Type type, string[] strings, out object result)
        {
            if (strings == null)
            {
                result = null;
                return false;
            }

            var method = GenericTryConvertToMethods.GetOrAdd(type, BuildNonGenericTryConvertLambda);
            var (success, methodResult) = method(strings);
            result = methodResult;
            return success;
        }

        /// <summary>
        /// Attempts to convert an array of strings to an array of the specified type.
        /// </summary>
        /// <typeparam name="TResult">The type resulting from the conversion of each
        /// element of <paramref name="strings"/>.</typeparam>
        /// <param name="strings">The array to convert.</param>
        /// <param name="result">When this method returns <see langword="true" />,
        /// the result of the conversion. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true" /> if the conversion is successful;
        /// otherwise, <see langword="false" />.</returns>
        public static bool TryConvertTo<TResult>(string[] strings, out TResult[] result)
        {
            if (strings == null)
            {
                result = null;
                return false;
            }

            var converter = TypeDescriptor.GetConverter(typeof(TResult));
            if (!converter.CanConvertFrom(typeof(string)))
            {
                result = null;
                return false;
            }

            try
            {
                result = new TResult[strings.Length];
                var i = 0;
                foreach (var str in strings)
                    result[i++] = (TResult)converter.ConvertFromInvariantString(str);

                return true;
            }
            catch (Exception e) when (!e.IsCriticalException())
            {
                result = null;
                return false;
            }
        }

        /// <summary>
        /// Converts an array of strings to an array of the specified type.
        /// </summary>
        /// <param name="type">The type resulting from the conversion of each
        /// element of <paramref name="strings"/>.</param>
        /// <param name="strings">The array to convert.</param>
        /// <returns>An array of <paramref name="type" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type" /> is <see langword="null" />.</exception>
        /// <exception cref="StringConversionException">The conversion of at least one
        /// of the elements of <paramref name="strings"/>was not successful.</exception>
        public static object ConvertTo(Type type, string[] strings)
        {
            if (strings == null)
                return null;

            var method = GenericConvertToMethods.GetOrAdd(type, BuildNonGenericConvertLambda);
            return method(strings);
        }

        /// <summary>
        /// Converts an array of strings to an array of the specified type.
        /// </summary>
        /// <typeparam name="TResult">The type resulting from the conversion of each
        /// element of <paramref name="strings"/>.</typeparam>
        /// <param name="strings">The array to convert.</param>
        /// <returns>An array of <typeparamref name="TResult" />.</returns>
        /// <exception cref="StringConversionException">The conversion of at least one
        /// of the elements of <paramref name="strings"/>was not successful.</exception>
        public static TResult[] ConvertTo<TResult>(string[] strings)
        {
            if (strings == null)
                return null;

            var converter = TypeDescriptor.GetConverter(typeof(TResult));
            var result = new TResult[strings.Length];
            var i = 0;
            try
            {
                foreach (var str in strings)
                    result[i++] = (TResult)converter.ConvertFromInvariantString(str);
            }
            catch (Exception e) when (!e.IsCriticalException())
            {
                throw new StringConversionException(typeof(TResult), e);
            }

            return result;
        }

        internal static Expression ConvertExpressionTo(Type type, Expression str)
        {
            var converter = TypeDescriptor.GetConverter(type);
            return converter.CanConvertFrom(typeof(string))
                ? Expression.Convert(
                    Expression.Call(Expression.Constant(converter), ConvertFromInvariantStringMethod, str),
                    type)
                : null;
        }

        private static Func<string[], (bool Success, object Result)> BuildNonGenericTryConvertLambda(Type type)
        {
            var methodInfo = TryConvertToInternalMethod.MakeGenericMethod(type);
            var parameter = Expression.Parameter(typeof(string[]));
            var body = Expression.Call(methodInfo, parameter);
            var lambda = Expression.Lambda<Func<string[], (bool Success, object Result)>>(body, parameter);
            return lambda.Compile();
        }

        private static (bool Success, object Result) TryConvertToInternal<TResult>(string[] strings)
        {
            var converter = TypeDescriptor.GetConverter(typeof(TResult));
            if (!converter.CanConvertFrom(typeof(string)))
                return (false, null);

            var result = new TResult[strings.Length];
            var i = 0;
            try
            {
                foreach (var str in strings)
                    result[i++] = (TResult)converter.ConvertFromInvariantString(str);

                return (true, result);
            }
            catch (Exception e) when (!e.IsCriticalException())
            {
                return (false, null);
            }
        }

        private static Func<string[], object> BuildNonGenericConvertLambda(Type type)
        {
            var methodInfo = ConvertToInternalMethod.MakeGenericMethod(type);
            var parameter = Expression.Parameter(typeof(string[]));
            var body = Expression.Call(methodInfo, parameter);
            var lambda = Expression.Lambda<Func<string[], object>>(body, parameter);
            return lambda.Compile();
        }

        private static object ConvertToInternal<TResult>(string[] strings)
        {
            var converter = TypeDescriptor.GetConverter(typeof(TResult));
            var result = new TResult[strings.Length];
            var i = 0;
            try
            {
                foreach (var str in strings)
                    result[i++] = (TResult)converter.ConvertFromInvariantString(str);

                return result;
            }
            catch (Exception e) when (!e.IsCriticalException())
            {
                throw new StringConversionException(typeof(TResult), e);
            }
        }
    }
}