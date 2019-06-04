using System.Net;

namespace EmbedIO
{
    partial class HttpException
    {
        public static HttpException Unauthorized() => new HttpException(HttpStatusCode.Unauthorized);

        public static HttpException Forbidden() => new HttpException(HttpStatusCode.Forbidden);

        public static HttpException BadRequest() => new HttpException(HttpStatusCode.BadRequest);

        public static HttpException NotFound() => new HttpException(HttpStatusCode.NotFound);

        public static HttpException MethodNotAllowed() => new HttpException(HttpStatusCode.MethodNotAllowed);

        public static HttpException NotAcceptable() => new HttpException(HttpStatusCode.NotAcceptable);

        public static HttpRedirectException Redirect(string location, int statusCode = (int)HttpStatusCode.Found)
            => new HttpRedirectException(location, statusCode);

        public static HttpRedirectException Redirect(string location, HttpStatusCode statusCode = HttpStatusCode.Found)
            => new HttpRedirectException(location, statusCode);
    }
}