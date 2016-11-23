namespace Unosquare.Labs.EmbedIO.Log
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides a simple logger with colored console output.
    /// </summary>
    public class SimpleConsoleLog : ILog
    {

        private static readonly ConcurrentQueue<OutputContext> OutputQueue = new ConcurrentQueue<OutputContext>();
        private static readonly Task OutputTask;

        /// <summary>
        /// Globally enables or disables Debug messages on the output of the console.
        /// By default, debug messages are disabled.
        /// </summary>
        public static bool IsDebugEnabled { get; set; } = Debugger.IsAttached;

        /// <summary>
        /// asynchronous output context
        /// </summary>
        private class OutputContext
        {
            public ConsoleColor OriginalColor { get; set; }
            public ConsoleColor OutputColor { get; set; }
            public string OutputText { get; set; }
        }

        /// <summary>
        /// Initializes the <see cref="SimpleConsoleLog"/> class.
        /// </summary>
        static SimpleConsoleLog()
        {
            OutputTask = Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if (OutputQueue.Count <= 0)
                        await Task.Delay(10);

                    while (OutputQueue.Count > 0)
                    {
                        OutputContext context;
                        if (OutputQueue.TryDequeue(out context) == false)
                            continue;

                        Console.ForegroundColor = context.OutputColor;
                        Console.WriteLine(context.OutputText);
                        Console.ResetColor();
                        Console.ForegroundColor = context.OriginalColor;
                    }
                }
            });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleConsoleLog"/> class.
        /// </summary>
        public SimpleConsoleLog()
        {
            // placeholder
        }

        /// <summary>
        /// Writes the given line. This method is used by all other methods and it is asynchronous.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The arguments.</param>
        private static void WriteLine(ConsoleColor color, string format, params object[] args)
        {
            var d = DateTime.Now;
            var dateTimeString =
                $"{d.Year:0000}-{d.Month:00}-{d.Day:00} {d.Hour:00}:{d.Minute:00}:{d.Second:00}.{d.Millisecond:000}";

            format = dateTimeString + "\t" + format;
            if (args == null) args = new object[] { };
            format = string.Format(format, args);

            var context = new OutputContext() { OriginalColor = Console.ForegroundColor, OutputColor = color, OutputText = format };
            OutputQueue.Enqueue(context);
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
            if (IsDebugEnabled)
                WriteLine(ConsoleColor.Green, format, args);
        }
    }
}