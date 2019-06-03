using System.Net;

namespace EmbedIO
{
    partial class HttpException
    {
        public static HttpException Unauthorized() => new HttpException(HttpStatusCode.Unauthorized);

        public static HttpException Forbidden() => new HttpException(HttpStatusCode.Forbidden);

        public static HttpException NotFound() => new HttpException(HttpStatusCode.NotFound);

        public static HttpRedirectException Redirect(string location, int statusCode = (int)HttpStatusCode.Found)
            => new HttpRedirectException(location, statusCode);

        public static HttpRedirectException Redirect(string location, HttpStatusCode statusCode = HttpStatusCode.Found)
            => new HttpRedirectException(location, statusCode);
    }
}