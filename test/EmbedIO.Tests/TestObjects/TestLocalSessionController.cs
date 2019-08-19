using System.Text;
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

        [Route(HttpVerbs.Get, "/getcookie")]
        public Task GetCookieC()
        {
            var cookie = new System.Net.Cookie(CookieName, CookieName);
            Response.Cookies.Add(cookie);

            return HttpContext.SendStringAsync(Response.Cookies[CookieName].Value, MimeType.PlainText, Encoding.UTF8);
        }

        [Route(HttpVerbs.Get, "/deletesession")]
        public Task DeleteSessionC()
        {
            HttpContext.Session.Delete();
            return HttpContext.SendStringAsync("Deleted", MimeType.PlainText, Encoding.UTF8);
        }

        [Route(HttpVerbs.Get, "/putdata")]
        public Task PutDataSession()
        {
            HttpContext.Session["sessionData"] = MyData;
            return HttpContext.SendStringAsync(HttpContext.Session["sessionData"].ToString(), MimeType.PlainText, Encoding.UTF8);
        }

        [Route(HttpVerbs.Get, "/getdata")]
        public Task GetDataSession()
            => HttpContext.SendStringAsync(HttpContext.Session["sessionData"]?.ToString() ?? string.Empty, MimeType.PlainText, Encoding.UTF8);
    }
}