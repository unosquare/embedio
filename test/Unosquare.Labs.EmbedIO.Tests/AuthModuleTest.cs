namespace Unosquare.Labs.EmbedIO.Tests
{
    using Constants;
    using Modules;
    using NUnit.Framework;
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    [TestFixture]
    public class AuthModuleTest : FixtureBase
    {
        public AuthModuleTest()
            : base(ws =>
                {
                    ws.RegisterModule(new AuthModule("root", "password1234"));
                    ws.RegisterModule(new FallbackModule((ctx, ct) => ctx.JsonResponse("OK")));
                },
                RoutingStrategy.Wildcard,
                true)
        {
            // placeholder
        }

        [Test]
        public async Task RequestWithValidCredentials_ReturnsOK()
        {
            var request = new TestHttpRequest(WebServerUrl);
            var byteArray = Encoding.ASCII.GetBytes("root:password1234");
            var authData = new System.Net.Http.Headers.AuthenticationHeaderValue("basic",
                Convert.ToBase64String(byteArray));
            request.Headers.Add("Authorization", authData.ToString());

            using (var response = await SendAsync(request))
            {
                Assert.AreEqual((int)HttpStatusCode.OK, response.StatusCode, "Status Code OK");
            }
        }

        [Test]
        public async Task RequestWithInvalidCredentials_ReturnsUnauthorized()
        {
            var request = new TestHttpRequest(WebServerUrl);
            var byteArray = Encoding.ASCII.GetBytes("root:password1233");
            var authData = new System.Net.Http.Headers.AuthenticationHeaderValue("basic",
                Convert.ToBase64String(byteArray));
            request.Headers.Add("Authorization", authData.ToString());

            using (var response = await SendAsync(request))
            {
                Assert.AreEqual((int)HttpStatusCode.Unauthorized, response.StatusCode, "Status Code Unauthorized");
            }
        }

        [Test]
        public async Task RequestWithNoAuthorizationHeader_ReturnsUnauthorized()
        {
            var request = new TestHttpRequest(WebServerUrl);

            using (var response = await SendAsync(request))
            {
                Assert.AreEqual((int)HttpStatusCode.Unauthorized, response.StatusCode, "Status Code Unauthorized");

            }
        }
    }
}