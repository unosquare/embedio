#if NETSTANDARD1_6
namespace System
{
    /// <summary>
    /// Defines the parts of a URI for the Uri.GetLeftPart method.
    /// </summary>
    public enum UriPartial
    {
        /// <summary>
        /// The scheme segment
        /// </summary>
        Scheme,
        /// <summary>
        /// The authority segment
        /// </summary>
        Authority,
        /// <summary>
        /// The path segment
        /// </summary>
        Path
    }
}
#endif