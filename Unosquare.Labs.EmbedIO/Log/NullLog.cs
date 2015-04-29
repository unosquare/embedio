namespace Unosquare.Labs.EmbedIO.Log
{
    using System;

    /// <summary>
    /// A Null Log. Useful if you don't want to pass a logger to the Web Server constructor.
    /// </summary>
    public class NullLog : ILog
    {
        public void Info(object message)
        {
            // placeholder
        }

        public void Error(object message)
        {
            // placeholder
        }

        public void Error(object message, Exception exception)
        {
            // placeholder
        }

        public void InfoFormat(string format, params object[] args)
        {
            // placeholder
        }

        public void WarnFormat(string format, params object[] args)
        {
            // placeholder
        }

        public void ErrorFormat(string format, params object[] args)
        {
            // placeholder
        }

        public void DebugFormat(string format, params object[] args)
        {
            // placeholder
        }
    }
}