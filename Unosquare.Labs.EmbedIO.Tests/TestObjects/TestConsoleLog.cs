namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
{
    using System;
    using System.Collections.Concurrent;

    public class TestConsoleLog : Unosquare.Labs.EmbedIO.Log.ILog
    {
        public static ConcurrentBag<string> Data = new ConcurrentBag<string>();

        public void Info(object message)
        {
            Data.Add(message.ToString());
        }

        public void Error(object message)
        {
            Data.Add(message.ToString());
        }

        public void Error(object message, Exception exception)
        {
            Data.Add(message.ToString());
            Data.Add(exception.ToString());
        }

        public void InfoFormat(string format, params object[] args)
        {
            Data.Add(String.Format(format, args));
        }

        public void WarnFormat(string format, params object[] args)
        {
            Data.Add(String.Format(format, args));
        }

        public void ErrorFormat(string format, params object[] args)
        {
            Data.Add(String.Format(format, args));
        }

        public void DebugFormat(string format, params object[] args)
        {
            Data.Add(String.Format(format, args));
        }
    }
}
