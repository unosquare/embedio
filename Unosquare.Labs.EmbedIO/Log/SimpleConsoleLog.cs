namespace Unosquare.Labs.EmbedIO.Log
{
    using System;
    using System.Threading;

    /// <summary>
    /// Provides a simple logger with colored console output.
    /// </summary>
    public class SimpleConsoleLog : ILog
    {
        /// <summary>
        /// Writes the given line. This method is used by all other methods and it is asynchronous.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The arguments.</param>
        private static void WriteLine(ConsoleColor color, string format, params object[] args)
        {
            var d = DateTime.Now;
            var dateTimeString = string.Format("{0}-{1}-{2} {3}:{4}:{5}.{6}",
                d.Year.ToString("0000"), d.Month.ToString("00"), d.Day.ToString("00"), d.Hour.ToString("00"),
                d.Minute.ToString("00"), d.Second.ToString("00"), d.Millisecond.ToString("000"));

            format = dateTimeString + "\t" + format;

            ThreadPool.QueueUserWorkItem((context) =>
            {
                var current = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(format, args);
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
            WriteLine(ConsoleColor.Gray, format, args);
        }

        /// <summary>
        /// Writes a Warning level message with format
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public virtual void WarnFormat(string format, params object[] args)
        {
            WriteLine(ConsoleColor.DarkYellow, format, args);
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
            WriteLine(ConsoleColor.Green, format, args);
        }
    }
}