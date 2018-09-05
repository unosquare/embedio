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

        public TestLocalSessionController(IHttpContext context)
            : base(context)
        {
        }

        [WebApiHandler(HttpVerbs.Get, "/getcookie")]
        public bool GetCookieC()
        {
            var cookie = new System.Net.Cookie(CookieName, CookieName);
            Response.Cookies.Add(cookie);

            return this.JsonResponse(Response.Cookies[CookieName]);
        }

        [WebApiHandler(HttpVerbs.Get, "/deletesession")]
        public bool DeleteSessionC()
        {
            this.DeleteSession();

            return this.JsonResponse("Deleted");
        }

        [WebApiHandler(HttpVerbs.Get, "/putdata")]
        public bool PutDataSession()
        {
            this.GetSession()?.Data.TryAdd("sessionData", MyData);

            return this.JsonResponse(this.GetSession().Data["sessionData"].ToString());
        }

        [WebApiHandler(HttpVerbs.Get, "/getdata")]
        public bool GetDataSession()
        {
            return this.JsonResponse(this.GetSession().Data.TryGetValue("sessionData", out var data)
                ? data.ToString()
                : string.Empty);
        }

        [WebApiHandler(HttpVerbs.Get, "/geterror")]
        public bool GetError()
        {
            return false;
        }
    }
}