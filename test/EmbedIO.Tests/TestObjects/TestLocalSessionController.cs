using System.Threading;
using System.Threading.Tasks;
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
        public Task<bool> GetCookieC()
        {
            var cookie = new System.Net.Cookie(CookieName, CookieName);
            Response.Cookies.Add(cookie);

            return Ok(Response.Cookies[CookieName]);
        }

        [RouteHandler(HttpVerbs.Get, "/deletesession")]
        public Task<bool> DeleteSessionC()
        {
            HttpContext.Session.Delete();

            return Ok("Deleted", MimeTypes.PlainTextType);
        }

        [RouteHandler(HttpVerbs.Get, "/putdata")]
        public Task<bool> PutDataSession()
        {
            HttpContext.Session["sessionData"] = MyData;

            return Ok(HttpContext.Session["sessionData"].ToString(), MimeTypes.PlainTextType);
        }

        [RouteHandler(HttpVerbs.Get, "/getdata")]
        public Task<bool> GetDataSession() =>
            Ok(HttpContext.Session["sessionData"]?.ToString() ?? string.Empty, MimeTypes.PlainTextType);
    }
}