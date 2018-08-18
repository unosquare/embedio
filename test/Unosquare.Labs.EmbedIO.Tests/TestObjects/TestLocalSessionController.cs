namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
{
    using Constants;
    using Modules;

    public class TestLocalSessionController : WebApiController
    {
        public const string DeleteSession = "deletesession";
        public const string PutData = "putdata";
        public const string GetData = "getdata";
        public const string GetCookie = "getcookie";

        public const string MyData = "MyData";
        public const string CookieName = "MyCookie";

        [WebApiHandler(HttpVerbs.Get, "/getcookie")]
        public bool GetCookieC(WebServer server, IHttpContext context)
        {
            var cookie = new System.Net.Cookie(CookieName, CookieName);
            context.Response.Cookies.Add(cookie);

            return context.JsonResponse(context.Response.Cookies[CookieName]);
        }

        [WebApiHandler(HttpVerbs.Get, "/deletesession")]
        public bool DeleteSessionC(WebServer server, IHttpContext context)
        {
            server.DeleteSession(context);

            return context.JsonResponse("Deleted");
        }

        [WebApiHandler(HttpVerbs.Get, "/putdata")]
        public bool PutDataSession(WebServer server, IHttpContext context)
        {
            server.GetSession(context).Data.TryAdd("sessionData", MyData);

            return context.JsonResponse(server.GetSession(context).Data["sessionData"].ToString());
        }

        [WebApiHandler(HttpVerbs.Get, "/getdata")]
        public bool GetDataSession(WebServer server, IHttpContext context)
        {
            return context.JsonResponse(server.GetSession(context).Data.TryGetValue("sessionData", out var data)
                ? data.ToString()
                : string.Empty);
        }

        [WebApiHandler(HttpVerbs.Get, "/geterror")]
        public bool GetError(WebServer server, IHttpContext context)
        {
            return false;
        }
    }
}