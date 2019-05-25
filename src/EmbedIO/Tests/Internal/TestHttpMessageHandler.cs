using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Utilities;

namespace EmbedIO.Tests.Internal
{
    internal sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly TestWebServer _server;

        public TestHttpMessageHandler(TestWebServer server)
        {
            _server = Validate.NotNull(nameof(server), server);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}