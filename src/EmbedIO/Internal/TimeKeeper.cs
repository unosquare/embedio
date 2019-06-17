using System.Diagnostics;

namespace EmbedIO.Internal
{
    /// <summary>
    /// Represents a wrapper around Stopwatch.
    /// </summary>
    public sealed class TimeKeeper
    {
        private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();

        private readonly long _start;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeKeeper"/> class.
        /// </summary>
        public TimeKeeper()
        {
            _start = Stopwatch.ElapsedMilliseconds;
        }

        /// <summary>
        /// Gets the elapsed time since the class was initialized.
        /// </summary>
        public long ElapsedTime => Stopwatch.ElapsedMilliseconds - _start;
    }
}