namespace EmbedIO
{
    // NOTE TO CONTRIBUTORS:
    // =====================
    // Do not reorder fields or change their values.
    // It is important that WebServerState values represent,
    // in ascending order, the stages of a web server's lifetime,
    // so that comparisons can be made; for example,
    // State < WebServerState.Listening means "not yet ready to accept requests".

    /// <summary>
    /// Represents the state of a web server.
    /// </summary>
    public enum WebServerState
    {
        /// <summary>
        /// The web server has not been started yet.
        /// </summary>
        Created,

        /// <summary>
        /// The web server has been started but it is still initializing.
        /// </summary>
        Loading,

        /// <summary>
        /// The web server is ready to accept incoming requests.
        /// </summary>
        Listening,

        /// <summary>
        /// The web server has been stopped.
        /// </summary>
        Stopped,
    }
}
