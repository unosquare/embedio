namespace Unosquare.Labs.EmbedIO.Log
{
    using System;

    /// <summary>
    /// A Null Log. Useful if you don't want to pass a real logger to the Web Server constructor.
    /// </summary>
    public class NullLog : ILog
    {
        /// <summary>
        /// Writes an Info level message
        /// </summary>
        /// <param name="message"></param>
        public void Info(object message)
        {
            // placeholder
        }

        /// <summary>
        /// Writes an Error level message
        /// </summary>
        /// <param name="message"></param>
        public void Error(object message)
        {
            // placeholder
        }

        /// <summary>
        /// Writes an Error message with a given Exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        public void Error(object message, Exception exception)
        {
            // placeholder
        }

        /// <summary>
        /// Writes an Info level message with format
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void InfoFormat(string format, params object[] args)
        {
            // placeholder
        }

        /// <summary>
        /// Writes a Warning level message with format
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WarnFormat(string format, params object[] args)
        {
            // placeholder
        }

        /// <summary>
        /// Writes an Error level message with format
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void ErrorFormat(string format, params object[] args)
        {
            // placeholder
        }

        /// <summary>
        /// Writes a Debug level message with format
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void DebugFormat(string format, params object[] args)
        {
            // placeholder
        }
    }
}