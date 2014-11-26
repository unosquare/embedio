namespace Unosquare.Labs.EmbedIO
{
    using log4net;
    using System;

    /// <summary>
    /// A Null Log. Useful if you don't want to pass a logger to the Web Server constructor.
    /// </summary>
    public class NullLog : ILog
    {
        public void Debug(object message, Exception exception)
        {
            // placeholder
        }

        public void Debug(object message)
        {
            // placeholder
        }

        public void DebugFormat(IFormatProvider provider, string format, params object[] args)
        {
            // placeholder
        }

        public void DebugFormat(string format, object arg0, object arg1, object arg2)
        {
            // placeholder
        }

        public void DebugFormat(string format, object arg0, object arg1)
        {
            // placeholder
        }

        public void DebugFormat(string format, object arg0)
        {
            // placeholder
        }

        public void DebugFormat(string format, params object[] args)
        {
            // placeholder
        }

        public void Error(object message, Exception exception)
        {
            // placeholder
        }

        public void Error(object message)
        {
            // placeholder
        }

        public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
        {
            // placeholder
        }

        public void ErrorFormat(string format, object arg0, object arg1, object arg2)
        {
            // placeholder
        }

        public void ErrorFormat(string format, object arg0, object arg1)
        {
            // placeholder
        }

        public void ErrorFormat(string format, object arg0)
        {
            // placeholder
        }

        public void ErrorFormat(string format, params object[] args)
        {
            // placeholder
        }

        public void Fatal(object message, Exception exception)
        {
            // placeholder
        }

        public void Fatal(object message)
        {
            // placeholder
        }

        public void FatalFormat(IFormatProvider provider, string format, params object[] args)
        {
            // placeholder
        }

        public void FatalFormat(string format, object arg0, object arg1, object arg2)
        {
            // placeholder
        }

        public void FatalFormat(string format, object arg0, object arg1)
        {
            // placeholder
        }

        public void FatalFormat(string format, object arg0)
        {
            // placeholder
        }

        public void FatalFormat(string format, params object[] args)
        {
            // placeholder
        }

        public void Info(object message, Exception exception)
        {
            // placeholder
        }

        public void Info(object message)
        {
            // placeholder
        }

        public void InfoFormat(IFormatProvider provider, string format, params object[] args)
        {
            // placeholder
        }

        public void InfoFormat(string format, object arg0, object arg1, object arg2)
        {
            // placeholder
        }

        public void InfoFormat(string format, object arg0, object arg1)
        {
            // placeholder
        }

        public void InfoFormat(string format, object arg0)
        {
            // placeholder
        }

        public void InfoFormat(string format, params object[] args)
        {
            // placeholder
        }

        public bool IsDebugEnabled
        {
            get { return false; }
        }

        public bool IsErrorEnabled
        {
            get { return false; }
        }

        public bool IsFatalEnabled
        {
            get { return false; }
        }

        public bool IsInfoEnabled
        {
            get { return false; }
        }

        public bool IsWarnEnabled
        {
            get { return false; }
        }

        public void Warn(object message, Exception exception)
        {
            // placeholder
        }

        public void Warn(object message)
        {
            // placeholder
        }

        public void WarnFormat(IFormatProvider provider, string format, params object[] args)
        {
            // placeholder
        }

        public void WarnFormat(string format, object arg0, object arg1, object arg2)
        {
            // placeholder
        }

        public void WarnFormat(string format, object arg0, object arg1)
        {
            // placeholder
        }

        public void WarnFormat(string format, object arg0)
        {
            // placeholder
        }

        public void WarnFormat(string format, params object[] args)
        {
            // placeholder
        }

        public log4net.Core.ILogger Logger
        {
            get { return null; }
        }
    }
}
