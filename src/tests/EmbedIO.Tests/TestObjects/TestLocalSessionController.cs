using System.Text;
using System.Threading.Tasks;
using EmbedIO.Routing;
using EmbedIO.Sessions;
using EmbedIO.WebApi;
using Swan;

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

        [Route(HttpVerb.Get, "/getcookie")]
        public Task GetCookie()
        {
            var cookie = new System.Net.Cookie(CookieName, CookieName);
            Response.Cookies.Add(cookie);

            return HttpContext.SendStringAsync(Response.Cookies[CookieName]!.Value, MimeType.PlainText, Encoding.UTF8);
        }

        [Route(HttpVerb.Get, "/deletesession")]
        public Task DeleteSession()
        {
            HttpContext.Session.Delete();
            return HttpContext.SendStringAsync("Deleted", MimeType.PlainText, Encoding.UTF8);
        }

        [Route(HttpVerb.Get, "/putdata")]
        public Task PutData()
        {
            HttpContext.Session["sessionData"] = MyData;
            return HttpContext.SendStringAsync(HttpContext.Session.GetOrDefault("sessionData", string.Empty), MimeType.PlainText, Encoding.UTF8);
        }

        [Route(HttpVerb.Get, "/getdata")]
        public Task GetData()
            => HttpContext.SendStringAsync(HttpContext.Session.GetOrDefault("sessionData", string.Empty), MimeType.PlainText, Encoding.UTF8);
    }
}