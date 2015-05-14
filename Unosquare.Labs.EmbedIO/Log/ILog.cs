namespace Unosquare.Labs.EmbedIO.Log
{
    using System;

    /// <summary>
    /// This interface represents all that is needed for logging
    /// </summary>
    public interface ILog
    {
        /// <summary>
        /// Writes an Info level message
        /// </summary>
        /// <param name="message"></param>
        void Info(object message);

        /// <summary>
        /// Writes an Error level message
        /// </summary>
        /// <param name="message"></param>
        void Error(object message);

        /// <summary>
        /// Writes an Error message with Exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        void Error(object message, Exception exception);

        /// <summary>
        /// Writes an Info level message with format
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        void InfoFormat(string format, params object[] args);

        /// <summary>
        /// Writes an Warn level message with format
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        void WarnFormat(string format, params object[] args);

        /// <summary>
        /// Writes an Error level message with format
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        void ErrorFormat(string format, params object[] args);

        /// <summary>
        /// Writes an Debug level message with format
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        void DebugFormat(string format, params object[] args);
    }
}
