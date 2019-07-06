using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Utilities;

namespace EmbedIO.Testing.Internal
{
    internal sealed partial class TestMessageHandler : HttpMessageHandler
    {
        private readonly TestWebServer _server;

        public TestMessageHandler(TestWebServer server)
        {
            _server = Validate.NotNull(nameof(server), server);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var serverRequest = new TestRequest(Validate.NotNull(nameof(request), request));
            var context = new TestContext(serverRequest);

            _server.EnqueueContext(context);

            if (!(context.Response is TestResponse serverResponse))
                throw new InvalidOperationException("The response object is invalid.");

            try
            {
                while (!serverResponse.IsClosed)
                    await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // ignore
            }

            var response = new HttpResponseMessage((HttpStatusCode) serverResponse.StatusCode) {
                RequestMessage = request,
                Version = serverResponse.ProtocolVersion,
                ReasonPhrase = serverResponse.StatusDescription,
                Content = serverResponse.Body == null ? null : new ByteArrayContent(serverResponse.Body),
            };
            foreach (var key in serverResponse.Headers.AllKeys)
            {
                switch (GetResponseHeaderType(key))
                {
                    case ResponseHeaderType.Content:
                    {
                        if (response.Content != null)
                            response.Content.Headers.Add(key, serverResponse.Headers.GetValues(key));

                        break;
                    }

                    case ResponseHeaderType.Response:
                        response.Headers.Add(key, serverResponse.Headers.GetValues(key));
                        break;
                }
            }

            return response;
        }

        private ResponseHeaderType GetResponseHeaderType(string name)
        {
            // Not all headers are created equal in System.Net.Http.
            // If a header is a "content" header, adding them to a HttpResponseMessage directly
            // will throw InvalidOperationException.
            // The list of known headers with their respective "header types"
            // is conveniently hidden in an internal class of System.Net.Http,
            // because nobody outside the .NET team will ever need them, right?
            // https://github.com/dotnet/corefx/blob/master/src/System.Net.Http/src/System/Net/Http/Headers/KnownHeaders.cs
            // Here are the "content" headers, extracted on 2019-07-06:
            switch (name)
            {
                // Content-Length is set autpmatically and shall not be touched
                case HttpHeaderNames.ContentLength:
                    return ResponseHeaderType.None;

                // These headers belong to Content
                case HttpHeaderNames.Allow:
                case HttpHeaderNames.ContentDisposition:
                case HttpHeaderNames.ContentEncoding:
                case HttpHeaderNames.ContentLanguage:
                case HttpHeaderNames.ContentLocation:
                case HttpHeaderNames.ContentMD5:
                case HttpHeaderNames.ContentRange:
                case HttpHeaderNames.ContentType:
                case HttpHeaderNames.Expires:
                case HttpHeaderNames.LastModified:
                    return ResponseHeaderType.Content;

                // All other headers belong to the response
                default:
                    return ResponseHeaderType.Response;
            }
        }
    }
}