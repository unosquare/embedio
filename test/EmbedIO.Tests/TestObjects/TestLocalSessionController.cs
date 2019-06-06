using System.Threading;
using EmbedIO.Routing;
using EmbedIO.WebApi;

namespace EmbedIO.Tests.TestObjects
{
    public class TestLocalSessionController : WebApiController
    {
        public const string DeleteSession = "api/deletesession";
        public const string PutData = "api/putdata";
        public const string GetData = "api/getdata";
        public const string GetCookie = "api/getcookie";

        public const string MyData = "MyData";
        public const string CookieName = "MyCookie";

        public TestLocalSessionController(IHttpContext context, CancellationToken cancellationToken)
            : base(context, cancellationToken)
        {
        }

        [RouteHandler(HttpVerbs.Get, "/getcookie")]
        public object GetCookieC()
        {
            var cookie = new System.Net.Cookie(CookieName, CookieName);
            Response.Cookies.Add(cookie);

            return Response.Cookies[CookieName];
        }

        [RouteHandler(HttpVerbs.Get, "/deletesession")]
        public object DeleteSessionC()
        {
            HttpContext.Session.Delete();
            Response.ContentType = MimeTypes.PlainTextType;
            return "Deleted";
        }

        [RouteHandler(HttpVerbs.Get, "/putdata")]
        public object PutDataSession()
        {
            HttpContext.Session["sessionData"] = MyData;
            Response.ContentType = MimeTypes.PlainTextType;
            return HttpContext.Session["sessionData"].ToString();
        }

        [RouteHandler(HttpVerbs.Get, "/getdata")]
        public object GetDataSession()
        {
            Response.ContentType = MimeTypes.PlainTextType;
            return HttpContext.Session["sessionData"]?.ToString() ?? string.Empty;
        }
    }
}