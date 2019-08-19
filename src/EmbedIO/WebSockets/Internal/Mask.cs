namespace EmbedIO.WebSockets.Internal
{
    /// <summary>
    /// Indicates whether the payload data of a WebSocket frame is masked.
    /// </summary>
    /// <remarks>
    /// The values of this enumeration are defined in
    /// <see href="http://tools.ietf.org/html/rfc6455#section-5.2">Section 5.2</see> of RFC 6455.
    /// </remarks>
    internal enum Mask : byte
    {
        /// <summary>
        /// Equivalent to numeric value 0. Indicates not masked.
        /// </summary>
        Off = 0x0,

        /// <summary>
        /// Equivalent to numeric value 1. Indicates masked.
        /// </summary>
        On = 0x1,
    }
}