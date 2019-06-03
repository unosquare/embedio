using System;
using System.Net;
using System.Threading.Tasks;

namespace EmbedIO
{
    public class HttpRedirectException : HttpException
    {
        public HttpRedirectException(string location, int statusCode = (int)HttpStatusCode.Found)
            : base(statusCode)
        {
            if (statusCode < 300 || statusCode > 399)
                throw new ArgumentException("Redirect status code is not valid.", nameof(statusCode));

            Location = location;
        }

        public HttpRedirectException(string location, HttpStatusCode statusCode = HttpStatusCode.Found)
            : this(location, (int)statusCode)
        {
        }

        public string Location { get; }

        protected override Task OnSendResponseAsync(IHttpContext context)
        {
            context.Redirect(Location, StatusCode);
            return Task.CompletedTask;
        }
    }
}