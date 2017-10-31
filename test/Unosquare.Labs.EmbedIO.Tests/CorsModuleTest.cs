namespace Unosquare.Labs.EmbedIO.Tests
{
    using Constants;
    using NUnit.Framework;
    using Swan.Formatters;
    using System.Net;
    using System.Threading.Tasks;
    using Modules;
    using TestObjects;

    [TestFixture]
    public class CorsModuleTest : FixtureBase
    {
        protected static object TestObj = new {Message = "OK"};

        public CorsModuleTest() : base((ws) =>
        {
            ws.EnableCors(
                "http://client.cors-api.appspot.com,http://unosquare.github.io,http://run.plnkr.co",
                "content-type",
                "post,get");

            ws.RegisterModule(new WebApiModule());
            ws.Module<WebApiModule>().RegisterController<TestController>();
            ws.RegisterModule(new FallbackModule((ctx, ct) => ctx.JsonResponse(TestObj)));
        })
        {

        }

        [Test]
        public async Task RequestFallback_ReturnsJsonObject()
        {
            var jsonBody = await GetString("invalidpath");

            Assert.AreEqual(Json.Serialize(TestObj, true), jsonBody, "Same content");
        }

        [Test]
        public async Task RequestOptionsVerb_ReturnsOK()
        {
            var request = (HttpWebRequest) WebRequest.Create(WebServerUrl + TestController.GetPath);
            request.Headers[Headers.Origin] = "http://unosquare.github.io";
            request.Headers[Headers.AccessControlRequestMethod] = "post";
            request.Headers[Headers.AccessControlRequestHeaders] = "content-type";
            request.Method = "OPTIONS";

            using (var response = (HttpWebResponse) await request.GetResponseAsync())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");
            }
        }
    }
}