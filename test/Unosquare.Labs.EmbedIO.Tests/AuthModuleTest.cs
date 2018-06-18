namespace Unosquare.Labs.EmbedIO.Tests
{
    using NUnit.Framework;
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Constants;
    using Modules;

    [TestFixture]
    public class AuthModuleTest : FixtureBase
    {
        public AuthModuleTest()
            : base(ws =>
            {
                ws.RegisterModule(new AuthModule("root", "password1234"));
                ws.RegisterModule(new FallbackModule((ctx, ct) => ctx.JsonResponse("OK")));
            }, RoutingStrategy.Wildcard)
        {
            // placeholder
        }

        [Test]
        public async Task RequestWithValidCredentials_ReturnsOK()
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl);
                var byteArray = Encoding.ASCII.GetBytes("root:password1234");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("basic", Convert.ToBase64String(byteArray));

                using (var response = await client.SendAsync(request))
                {
                    Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");
                }
            }
        }

        [Test]
        public async Task RequestWithInvalidCredentials_ReturnsUnauthorize()
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl);
                var byteArray = Encoding.ASCII.GetBytes("root:password1233");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("basic", Convert.ToBase64String(byteArray));

                using (var response = await client.SendAsync(request))
                {
                    Assert.AreEqual(response.StatusCode, HttpStatusCode.Unauthorized, "Status Code Unauthorized");
                }
            }
        }

        [Test]
        public async Task RequestWithNoAuthorizationHeader_ReturnsUnauthorize()
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl);

                using (var response = await client.SendAsync(request))
                {
                    Assert.AreEqual(response.StatusCode, HttpStatusCode.Unauthorized, "Status Code Unauthorized");
                }
            }
        }
    }
}