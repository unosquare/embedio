using System;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// <para>Indicates to static code analyzers that a parameter is guaranteed not to be <see langword="null"/>
    /// after a method returns.</para>
    /// <para>The presence of this attribute on a method parameter also relaxes null-validation
    /// requirements, thus suppressing the <see href="https://docs.microsoft.com/en-us/visualstudio/code-quality/ca1062">CA1062 warning</see>
    /// on the parameter.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class ValidatedNotNullAttribute : Attribute
    {
    }
}