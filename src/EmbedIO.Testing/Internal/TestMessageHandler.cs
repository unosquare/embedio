using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Routing;
using EmbedIO.Utilities;

namespace EmbedIO.Testing.Internal
{
    internal sealed partial class TestMessageHandler : HttpMessageHandler
    {
        private readonly IHttpContextHandler _handler;

        public TestMessageHandler(IHttpContextHandler handler)
        {
            _handler = Validate.NotNull(nameof(handler), handler);
            CookieContainer = new CookieContainer();
        }

        public CookieContainer CookieContainer { get; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var serverRequest = new TestRequest(Validate.NotNull(nameof(request), request));
            var cookiesFromContainer = CookieContainer.GetCookieHeader(serverRequest.Url);
            if (!string.IsNullOrEmpty(cookiesFromContainer))
                serverRequest.Headers.Add(HttpHeaderNames.Cookie, cookiesFromContainer);

            var context = new TestContext(serverRequest);
            context.CancellationToken = cancellationToken;
            context.Route = RouteMatch.UnsafeFromRoot(UrlPath.Normalize(serverRequest.Url.AbsolutePath, false));
            await _handler.HandleContextAsync(context).ConfigureAwait(false);
            var serverResponse = context.TestResponse;
            var responseCookies = serverResponse.Headers.Get(HttpHeaderNames.SetCookie);
            if (!string.IsNullOrEmpty(responseCookies))
                CookieContainer.SetCookies(serverRequest.Url, responseCookies);

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
                        response.Content?.Headers.Add(key, serverResponse.Headers.GetValues(key));
                        break;
                    case ResponseHeaderType.Response:
                        response.Headers.Add(key, serverResponse.Headers.GetValues(key));
                        break;
                }
            }

            return response;
        }

        private static ResponseHeaderType GetResponseHeaderType(string name)
        {
            // Not all headers are created equal in System.Net.Http.
            // If a header is a "content" header, adding it to a HttpResponseMessage directly
            // will cause an InvalidOperationException.
            // The list of known headers with their respective "header types"
            // is conveniently hidden in an internal class of System.Net.Http,
            // because nobody outside the .NET team will ever need them, right?
            // https://github.com/dotnet/corefx/blob/master/src/System.Net.Http/src/System/Net/Http/Headers/KnownHeaders.cs
            // Here are the "content" headers, extracted on 2019-07-06:
            switch (name)
            {
                // Content-Length is set automatically and shall not be touched
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