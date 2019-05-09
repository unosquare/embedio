using EmbedIO.Utilities;

namespace EmbedIO
{
    public static class HttpContextExtensions
    {
        public static SessionInfo GetSession(this IHttpContext @this)
            => Validate.NotNull(nameof(@this), @this).Items[HttpContext.SessionKey] as SessionInfo;
    }
}