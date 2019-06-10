using System;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.Swan;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// Schedule an action to be periodically executed on the thread pool.
    /// </summary>
    public class PeriodicTask : IDisposable
    {
        /// <summary>
        /// <para>The minimum interval between action invocations.</para>
        /// <para>The value of this field is equal to 100 milliseconds.</para>
        /// </summary>
        public static readonly TimeSpan MinInterval = TimeSpan.FromMilliseconds(100);

        private readonly Func<CancellationToken, Task> _action;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private TimeSpan _interval;

        /// <summary>
        /// Initializes a new instance of the <see cref="PeriodicTask"/> class.
        /// </summary>
        /// <param name="interval">The interval between invocations of <paramref name="action"/>.</param>
        /// <param name="action">The callback to invoke periodically.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel operations.</param>
        public PeriodicTask(TimeSpan interval, Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
        {
            _action = Validate.NotNull(nameof(action), action);
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _interval = ValidateInterval(nameof(interval), interval);

            Task.Run(ActionLoop);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="PeriodicTask"/> class.
        /// </summary>
        ~PeriodicTask()
        {
            Dispose(false);
        }

        /// <summary>
        /// <para>Gets or sets the interval between periodic action invocations.</para>
        /// <para>Changes to this property take effect after next action invocation.</para>
        /// </summary>
        /// <seealso cref="MinInterval"/>
        public TimeSpan Interval
        {
            get => _interval;
            set => _interval = ValidateInterval(nameof(value), value);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }
        }

        private TimeSpan ValidateInterval(string argumentName, TimeSpan value)
            => value < MinInterval ? MinInterval : value;

        private async Task ActionLoop()
        {
            for (;;)
            {
                try
                {
                    await Task.Delay(Interval, _cancellationTokenSource.Token).ConfigureAwait(false);
                    await _action(_cancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (_cancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }
                catch (TaskCanceledException) when (_cancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    ex.Log(nameof(PeriodicTask));
                }
            }
        }
    }
}