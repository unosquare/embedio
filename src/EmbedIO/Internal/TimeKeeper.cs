using System.Diagnostics;

namespace EmbedIO.Internal
{
    internal sealed class TimeKeeper
    {
        private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();

        private readonly long _start;

        public TimeKeeper()
        {
            _start = Stopwatch.ElapsedMilliseconds;
        }

        public long ElapsedTime => Stopwatch.ElapsedMilliseconds - _start;
    }
}