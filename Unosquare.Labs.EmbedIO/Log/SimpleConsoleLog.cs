namespace Unosquare.Labs.EmbedIO.Log
{
    using System;

    /// <summary>
    /// Simple logger with output to Console
    /// </summary>
    public class SimpleConsoleLog : ILog
    {
        private static void WriteLine(ConsoleColor color, string format, params object[] args)
        {
            var current = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(format, args);
            Console.ForegroundColor = current;
        }

        public virtual void Info(object message)
        {
            InfoFormat(message.ToString(), null);
        }

        public virtual void Error(object message)
        {
            ErrorFormat(message.ToString(), null);
        }

        public virtual void Error(object message, Exception exception)
        {
            ErrorFormat(message.ToString(), null);
            ErrorFormat(exception.ToString(), null);
        }

        public virtual void InfoFormat(string format, params object[] args)
        {
            WriteLine(ConsoleColor.Blue, format, args);
        }

        public virtual void WarnFormat(string format, params object[] args)
        {
            WriteLine(ConsoleColor.Yellow, format, args);
        }

        public virtual void ErrorFormat(string format, params object[] args)
        {
            WriteLine(ConsoleColor.Red, format, args);
        }

        public virtual void DebugFormat(string format, params object[] args)
        {
            WriteLine(ConsoleColor.Cyan, format, args);
        }
    }
}
