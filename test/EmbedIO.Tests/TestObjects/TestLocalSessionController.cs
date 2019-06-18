using System.Text;
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

        [Route(HttpVerbs.Get, "/getcookie")]
        public Task<bool> GetCookieC()
        {
            var cookie = new System.Net.Cookie(CookieName, CookieName);
            Response.Cookies.Add(cookie);

            return HttpContext.SendStringAsync(Response.Cookies[CookieName].Value, MimeType.PlainText, Encoding.UTF8, CancellationToken);
        }

        [Route(HttpVerbs.Get, "/deletesession")]
        public Task<bool> DeleteSessionC()
        {
            HttpContext.Session.Delete();
            return HttpContext.SendStringAsync("Deleted", MimeType.PlainText, Encoding.UTF8, CancellationToken);
        }

        [Route(HttpVerbs.Get, "/putdata")]
        public Task<bool> PutDataSession()
        {
            HttpContext.Session["sessionData"] = MyData;
            return HttpContext.SendStringAsync(HttpContext.Session["sessionData"].ToString(), MimeType.PlainText, Encoding.UTF8, CancellationToken);
        }

        [Route(HttpVerbs.Get, "/getdata")]
        public Task<bool> GetDataSession()
            => HttpContext.SendStringAsync(HttpContext.Session["sessionData"]?.ToString() ?? string.Empty, MimeType.PlainText, Encoding.UTF8, CancellationToken);
    }
}