namespace Unosquare.Labs.EmbedIO.Samples
{
    using log4net;
    using log4net.Appender;
    using log4net.Core;
    using System;
    using System.Collections.Generic;

    static public class Logger
    {
        private const string LogPattern = "%-20date [%thread] %-5level %-20logger %message%newline";

        /// <summary>
        /// Retrieves a Logger for the given generic type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        static public ILog For<T>()
        {
            if (LogManager.GetRepository().Configured == false)
                ConfigureLogging();

            return LogManager.GetLogger(typeof(T));
        }

        /// <summary>
        /// Shutdowns the logging Subsystem.
        /// </summary>
        static public void Shutdown()
        {
            log4net.Repository.ILoggerRepository repository = LogManager.GetRepository();
            if (repository != null)
                repository.Shutdown();

            LogManager.Shutdown();
        }

        /// <summary>
        /// Configures the logging subsystem.
        /// </summary>
        private static void ConfigureLogging()
        {
            var layout = new log4net.Layout.PatternLayout(LogPattern);
            layout.ActivateOptions();

            // Create a list of appenders
            var appenders = new List<AppenderSkeleton>();


            var consoleAppender = new ManagedColoredConsoleAppender()
            {
                Layout = layout,
                Threshold = Level.Debug,
                Target = "Console.Out"
            };

            consoleAppender.AddMapping(new ManagedColoredConsoleAppender.LevelColors()
            {
                Level = Level.Info,
                ForeColor = ConsoleColor.Gray
            });
            consoleAppender.AddMapping(new ManagedColoredConsoleAppender.LevelColors()
            {
                Level = Level.Debug,
                ForeColor = ConsoleColor.Green
            });
            consoleAppender.AddMapping(new ManagedColoredConsoleAppender.LevelColors()
            {
                Level = Level.Warn,
                ForeColor = ConsoleColor.DarkYellow
            });
            consoleAppender.AddMapping(new ManagedColoredConsoleAppender.LevelColors()
            {
                Level = Level.Error,
                ForeColor = ConsoleColor.Red
            });

            appenders.Add(consoleAppender);

            // Configure the appenders in the list based on the build.
            foreach (var appender in appenders)
            {
                appender.AddFilter(new log4net.Filter.LevelRangeFilter()
                {
#if DEBUG
                    LevelMin = Level.Debug,
#else
                    LevelMin = Level.Info,
#endif
                    LevelMax = Level.Fatal
                });

                appender.ActivateOptions();
            }

            // Finally, perform the log configuration
            log4net.Config.BasicConfigurator.Configure(appenders.ToArray());
        }
    }
}
