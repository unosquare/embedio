using System;
using System.Net;
using System.Threading.Tasks;

namespace EmbedIO
{
    public partial class HttpException : Exception
    {
        public HttpException(int statusCode)
        {
            StatusCode = statusCode;
        }

        public HttpException(HttpStatusCode statusCode)
            : this((int)statusCode)
        {
        }

        public int StatusCode { get; }

        public Task SendResponseAsync(IHttpContext context)
        {
            context.Response.SetEmptyResponse(StatusCode);
            return OnSendResponseAsync(context);
        }

        protected virtual Task OnSendResponseAsync(IHttpContext context) => Task.CompletedTask;
    }
}