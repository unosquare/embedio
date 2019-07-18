using System;
using System.Threading.Tasks;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// Provides extension methods for Reflection classes.
    /// </summary>
    public static class ReflectionExtensions
    {
        /// <summary>
        /// Determines whether the specified type is a generic task type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="resultType">Type of the result.</param>
        /// <returns>
        ///   <c>true</c> if the specified type is a generic task type; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsGenericTaskType(this Type type, out Type resultType)
        {
            resultType = null;

            if (!type.IsConstructedGenericType)
                return false;

            if (type.GetGenericTypeDefinition() != typeof(Task<>))
                return false;

            resultType = type.GetGenericArguments()[0];
            return true;
        }
    }
}
