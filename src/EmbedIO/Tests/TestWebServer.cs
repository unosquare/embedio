using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Constants;
using Unosquare.Swan;

namespace EmbedIO.Tests
{
    /// <summary>
    /// Represents our tiny web server used to handle requests for testing environments.
    ///
    /// Use this <c>IWebServer</c> implementation to run your unit tests.
    /// </summary>
    public class TestWebServer : WebServerBase
    {
        private readonly Queue<IHttpContextImpl> _contexts = new Queue<IHttpContextImpl>();

        private bool _listening;

        private TaskCompletionSource<IHttpContextImpl> _pendingDequeue;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestWebServer"/> class.
        /// </summary>
        public TestWebServer()
        {
            Terminal.Settings.DisplayLoggingMessageType = LogMessageType.None;
            _listening = true;
            State = WebServerState.Listening;
        }

        /// <summary>
        /// Gets the test HTTP Client.
        /// </summary>
        /// <returns>A new instance of the TestHttpClient.</returns>
        public TestHttpClient GetClient() => new TestHttpClient(this);

        internal void EnqueueContext(IHttpContextImpl context)
        {
            if (!_listening)
                throw new InvalidOperationException("Web server is not listening any longer.");

            TaskCompletionSource<IHttpContextImpl> currentDequeue = null;
            lock (_contexts)
            {
                if (_pendingDequeue != null)
                {
                    currentDequeue = _pendingDequeue;
                    _pendingDequeue = null;
                }
                else
                {
                    _contexts.Enqueue(context);
                }
            }

            currentDequeue?.SetResult(context);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                TaskCompletionSource<IHttpContextImpl> currentDequeue = null;
                lock (_contexts)
                {
                    if (_pendingDequeue != null)
                    {
                        currentDequeue = _pendingDequeue;
                        _pendingDequeue = null;
                    }
                }

                currentDequeue?.SetException(new ObjectDisposedException(nameof(TestWebServer)));
            }

            base.Dispose(disposing);
        }

        /// <inheritdoc />
        protected override bool ShouldProcessMoreRequests()
        {
            lock (_contexts)
            {
                return _listening;
            }
        }

        /// <inheritdoc />
        protected override Task<IHttpContextImpl> GetContextAsync(CancellationToken cancellationToken)
        {
            lock (_contexts)
            {
                if (_contexts.Count > 0)
                {
                    return Task.FromResult(_contexts.Dequeue());
                }

                if (_pendingDequeue != null)
                    throw new InvalidOperationException("Trying to dequeue two contexts at the same time.");

                _pendingDequeue = new TaskCompletionSource<IHttpContextImpl>();
                return _pendingDequeue.Task;
            }
        }

        /// <inheritdoc />
        protected override void OnException()
        {
            lock (_contexts)
            {
                _listening = false;
            }
        } 
    }
}
