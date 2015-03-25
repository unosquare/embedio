namespace Unosquare.Labs.EmbedIO
{
    using System;

    /// <summary>
    /// This interface represents all needed for logging
    /// </summary>
    public interface ILog
    {
        void Info(object message);

        void Error(object message);

        void Error(object message, Exception exception);

        void InfoFormat(string format, params object[] args);

        void WarnFormat(string format, params object[] args);

        void ErrorFormat(string format, params object[] args);

        void DebugFormat(string format, params object[] args);
    }
}
