using System.Text;
using System.Threading.Tasks;
using EmbedIO.Routing;
using EmbedIO.Sessions;
using EmbedIO.WebApi;

namespace EmbedIO.Tests.TestObjects
{
    public class TestLocalSessionController : WebApiController
    {
        public const string DeleteSessionPath = "api/deletesession";
        public const string PutDataPath = "api/putdata";
        public const string GetDataPath = "api/getdata";
        public const string GetCookiePath = "api/getcookie";

        public const string MyData = "MyData";
        public const string CookieName = "MyCookie";

        [Route(HttpVerbs.Get, "/getcookie")]
        public Task GetCookieC()
        {
            var cookie = new System.Net.Cookie(CookieName, CookieName);
            Response.Cookies.Add(cookie);

            return HttpContext.SendStringAsync(Response.Cookies[CookieName].Value, MimeType.PlainText, WebServer.DefaultEncoding);
        }

        [Route(HttpVerbs.Get, "/deletesession")]
        public Task DeleteSessionC()
        {
            HttpContext.Session.Delete();
            return HttpContext.SendStringAsync("Deleted", MimeType.PlainText, WebServer.DefaultEncoding);
        }

        [Route(HttpVerbs.Get, "/putdata")]
        public Task PutDataSession()
        {
            HttpContext.Session["sessionData"] = MyData;
            return HttpContext.SendStringAsync(HttpContext.Session.GetOrDefault("sessionData", string.Empty), MimeType.PlainText, WebServer.DefaultEncoding);
        }

        [Route(HttpVerbs.Get, "/getdata")]
        public Task GetDataSession()
            => HttpContext.SendStringAsync(HttpContext.Session["sessionData"]?.ToString() ?? string.Empty, MimeType.PlainText, WebServer.DefaultEncoding);
    }
}
