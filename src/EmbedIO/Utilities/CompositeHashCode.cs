using System;
using System.Collections.Generic;
using System.Linq;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// <para>Provides a way for types that override <see cref="object.GetHashCode"/>
    /// to correctly generate a hash code from the actual status of an instance.</para>
    /// <para><c>CompositeHashCode</c> must be used ONLY as a helper when implementing
    /// <see cref="IEquatable{T}">IEquatable&lt;T&gt;</see> in a STANDARD way, i.e. when:</para>
    /// <list type="bullet">
    ///     <item><description>two instances having the same hash code are actually
    ///     interchangeable, i.e. they represent exactly the same object (for instance,
    ///     they should not coexist in a
    ///     <see cref="SortedSet{T}">SortedSet</see>);</description></item>
    ///     <item><description><see cref="object.GetHashCode">GetHashCode</see> and
    ///     <see cref="object.Equals(object)">Equals</see> are BOTH overridden, and the <c>Equals</c>
    ///     override either calls to the <see cref="IEquatable{T}.Equals(T)">IEquatable&lt;T&gt;.Equals</see>
    ///     (recommended) or performs exactly the same equality checks;</description></item>
    ///     <item><description>only "standard" equality checks are performed, i.e. by means of the
    ///     <c>==</c> operator, <see cref="IEquatable{T}">IEquatable&lt;T&gt;</see> interfaces, and
    ///     the <see cref="object.Equals(object)">Equals</see> method (for instance, this excludes case-insensitive
    ///     and/or culture-dependent string comparisons);
    ///     </description></item>
    ///     <item><description>the hash code is constructed (via <c>Using</c> calls) from the very same
    ///     fields and / or properties that are checked for equality.</description></item>
    /// </list>
    /// <para>For hashing to work correctly, all fields and/or properties involved in hashing must either
    /// be immutable, or at least not change while an object is referenced in a hashtable.
    /// This does not refer just to <c>System.Collections.Hashtable</c>; the .NET
    /// Framework makes a fairly extensive use of hashing, for example in
    /// <see cref="SortedSet{T}">SortedSet&lt;T&gt;</see>
    /// and in various parts of LINQ. As a thumb rule, an object must stay the same during the execution of a
    /// LINQ query on an <see cref="IEnumerable{T}">IEnumerable</see>
    /// in which it is contained, as well as all the time it is referenced in a <c>Hashtable</c> or <c>SortedSet</c>.</para>
    /// </summary>
    /// <example>
    /// <para>The following code constitutes a minimal use case for <c>CompositeHashCode</c>, as well
    /// as a reference for standard <see cref="System.IEquatable{T}">IEquatable&lt;T&gt;</see> implementation.</para>
    /// <para>Notice that all relevant properties are immutable; this is not, as stated in the summary,
    /// an absolute requirement, but it surely helps and should be done every time it makes sense.</para>
    /// <code>using System;
    /// using EmbedIO.Utilities;
    ///
    /// namespace Example
    /// {
    ///     public class Person : IEquatable&lt;Person&gt;
    ///     {
    ///         public string Name { get; private set; }
    ///
    ///         public int Age { get; private set; }
    ///
    ///         public Person(string name, int age)
    ///         {
    ///             Name = name;
    ///             Age = age;
    ///         }
    ///
    ///         public override int GetHashCode() => CompositeHashCode.Using(Name, Age);
    ///
    ///         public override bool Equals(object obj) => obj is Person other &amp;&amp; Equals(other);
    ///
    ///         public bool Equals(Person other)
    ///             => other != null
    ///             &amp;&amp; other.Name == Name
    ///             &amp;&amp; other.Age == Age;
    ///     }
    /// }</code>
    /// </example>
    public static class CompositeHashCode
    {
        #region Private constants

        private const int InitialSeed = 17;
        private const int Multiplier = 29;

        #endregion

        #region Public API

        /// <summary>
        /// Computes a hash code, taking into consideration the values of the specified
        /// fields and/oror properties as part of an object's state. See the
        /// <see cref="CompositeHashCode">example</see>.
        /// </summary>
        /// <param name="fields">The values of the fields and/or properties.</param>
        /// <returns>The computed has code.</returns>
        public static int Using(params object[] fields)
        {
            unchecked
            {
                return fields.Where(f => !(f is null))
                    .Aggregate(InitialSeed, (current, field) => (Multiplier * current) + field.GetHashCode());
            }
        }

        #endregion
    }
}