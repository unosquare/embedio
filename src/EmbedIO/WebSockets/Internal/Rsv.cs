namespace EmbedIO.WebSockets.Internal
{
    /// <summary>
    /// Indicates whether each RSV (RSV1, RSV2, and RSV3) of a WebSocket frame is non-zero.
    /// </summary>
    /// <remarks>
    /// The values of this enumeration are defined in
    /// <see href="http://tools.ietf.org/html/rfc6455#section-5.2">Section 5.2</see> of RFC 6455.
    /// </remarks>
    internal enum Rsv : byte
    {
        /// <summary>
        /// Equivalent to numeric value 0. Indicates zero.
        /// </summary>
        Off = 0x0,

        /// <summary>
        /// Equivalent to numeric value 1. Indicates non-zero.
        /// </summary>
        On = 0x1,
    }
}