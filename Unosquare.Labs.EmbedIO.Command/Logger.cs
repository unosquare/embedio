namespace Unosquare.Labs.EmbedIO.Command
{
    using System;

    public class Logger : Unosquare.Labs.EmbedIO.ILog
    {
        public void Info(object message)
        {
            InfoFormat(message.ToString(), null);
        }

        public void Error(object message)
        {
            ErrorFormat(message.ToString(), null);
        }

        public void Error(object message, Exception exception)
        {
            ErrorFormat(message.ToString(), null);
            ErrorFormat(exception.ToString(), null);
        }

        private void WriteLine(ConsoleColor color, string format, params object[] args)
        {
            var current = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(format, args);
            Console.ForegroundColor = current;
        }

        public void InfoFormat(string format, params object[] args)
        {
            WriteLine(ConsoleColor.Blue, format, args);
        }

        public void WarnFormat(string format, params object[] args)
        {
            WriteLine(ConsoleColor.Yellow, format, args);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            WriteLine(ConsoleColor.Red, format, args);
        }

        public void DebugFormat(string format, params object[] args)
        {
            WriteLine(ConsoleColor.Cyan, format, args);
        }
    }
}
