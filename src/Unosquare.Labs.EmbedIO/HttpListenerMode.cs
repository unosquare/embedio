namespace Unosquare.Labs.EmbedIO
{
    /// <summary>
    /// Enums all the HTTP listener availables.
    /// </summary>
    public enum HttpListenerMode
    {
        /// <summary>
        /// The EmbedIO mode
        /// </summary>
        EmbedIO,

#if !NETSTANDARD1_3 && !UWP
        /// <summary>
        /// The Microsoft mode
        /// </summary>
        Microsoft,
#endif
    }
}