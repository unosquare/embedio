namespace EmbedIO.Sessions
{
    /// <summary>
    /// Represents a session proxy, i.e. an object that provides
    /// the same interface as a session object, plus a basic interface
    /// to a session manager.
    /// </summary>
    /// <remarks>
    /// A session proxy can be used just as if it were a session object.
    /// A session is automatically created wherever its data are accessed.
    /// </remarks>
    /// <seealso cref="ISession" />
    public interface ISessionProxy : ISession
    {
        /// <summary>
        /// Gets a value indicating whether a session exists for the current context.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if a session exists; otherwise, <see langword="false"/>.
        /// </value>
        bool Exists { get; }

        /// <summary>
        /// Deletes the session for the current context.
        /// </summary>
        void Delete();

        /// <summary>
        /// Deletes the session for the current context and creates a new one.
        /// </summary>
        void Regenerate();
    }
}