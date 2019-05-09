using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Constants;
using Unosquare.Swan;
using EmbedIO.Net;

namespace EmbedIO.Tests
{
    /// <summary>
    /// Represents our tiny web server used to handle requests for testing environments.
    ///
    /// Use this <c>IWebServer</c> implementation to run your unit tests.
    /// </summary>
    public class TestWebServer : WebServerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestWebServer"/> class.
        /// </summary>
        public TestWebServer()
        {
            Terminal.Settings.DisplayLoggingMessageType = LogMessageType.None;
            State = WebServerState.Listening;
        }

        /// <summary>
        /// Gets the HTTP contexts.
        /// </summary>
        /// <value>
        /// The HTTP contexts.
        /// </value>
        public ConcurrentQueue<IHttpContext> HttpContexts { get; } = new ConcurrentQueue<IHttpContext>();

        /// <summary>
        /// Gets the test HTTP Client.
        /// </summary>
        /// <returns>A new instance of the TestHttpClient.</returns>
        public TestHttpClient GetClient() => new TestHttpClient(this);

        /// <inheritdoc />
        protected override async Task RunInternalAsync(CancellationToken ct)
        {
            try
            {
                var context = await GetContextAsync(ct).ConfigureAwait(false);

                if (ct.IsCancellationRequested)
                    return;

#pragma warning disable CS4014
                HandleClientRequest(context, ct);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException || ex is ObjectDisposedException ||
                    ex is HttpListenerException)
                {
                    if (!ct.IsCancellationRequested)
                        throw;

                    return;
                }

                ex.Log(nameof(WebServer));
            }
        }

        private async Task<IHttpContext> GetContextAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (HttpContexts.TryDequeue(out var entry)) return entry;

                await Task.Delay(100, ct).ConfigureAwait(false);
            }

            return null;
        }
    }
}
