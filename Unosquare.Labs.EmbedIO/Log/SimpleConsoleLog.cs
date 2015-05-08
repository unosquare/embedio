namespace Unosquare.Labs.EmbedIO.Log
{
    using System;
    using System.Threading;

    /// <summary>
    /// Simple logger with output to Console
    /// </summary>
    public class SimpleConsoleLog : ILog
    {
        private static void WriteLine(ConsoleColor color, string format, params object[] args)
        {
            ThreadPool.QueueUserWorkItem((context) =>
            {
                var current = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(">> " + format, args);
                Console.ForegroundColor = current;
            });
        }

        /// <summary>
        /// Writes an Info level message
        /// </summary>
        /// <param name="message"></param>
        public virtual void Info(object message)
        {
            InfoFormat(message.ToString(), null);
        }

        /// <summary>
        /// Writes an Error level message
        /// </summary>
        /// <param name="message"></param>
        public virtual void Error(object message)
        {
            ErrorFormat(message.ToString(), null);
        }

        /// <summary>
        /// Writes an Error message with Exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        public virtual void Error(object message, Exception exception)
        {
            ErrorFormat(message.ToString(), null);
            ErrorFormat(exception.ToString(), null);
        }

        /// <summary>
        /// Writes an Info level message with format
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public virtual void InfoFormat(string format, params object[] args)
        {
            WriteLine(ConsoleColor.Blue, format, args);
        }

        /// <summary>
        /// Writes an Warn level message with format
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public virtual void WarnFormat(string format, params object[] args)
        {
            WriteLine(ConsoleColor.Yellow, format, args);
        }

        /// <summary>
        /// Writes an Error level message with format
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public virtual void ErrorFormat(string format, params object[] args)
        {
            WriteLine(ConsoleColor.Red, format, args);
        }

        /// <summary>
        /// Writes an Debug level message with format
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public virtual void DebugFormat(string format, params object[] args)
        {
            WriteLine(ConsoleColor.Cyan, format, args);
        }
    }
}