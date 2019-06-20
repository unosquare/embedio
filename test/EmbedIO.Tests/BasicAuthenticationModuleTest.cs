using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using EmbedIO.Authentication;
using EmbedIO.Testing;
using EmbedIO.Utilities;
using NUnit.Framework;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class BasicAuthenticationModuleTest : EndToEndFixtureBase
    {
        private const string UserName = "root";
        private const string Password = "password1234";

        public BasicAuthenticationModuleTest()
            : base(ws =>
                {
                    ws.Modules.Add(new BasicAuthenticationModule("/").WithAccount(UserName, Password));
                    ws.OnAny((ctx, path, ct) =>
                    {
                        ctx.Response.SetEmptyResponse((int)HttpStatusCode.OK);

                        return Task.FromResult(true);
                    });
                },
                true)
        {
            // placeholder
        }

        [Test]
        public async Task RequestWithValidCredentials_ReturnsOK()
        {
            var response = await MakeRequest(UserName, Password);
            Assert.AreEqual((int)HttpStatusCode.OK, response.StatusCode, "Status Code OK");
        }

        [Test]
        public async Task RequestWithInvalidCredentials_ReturnsUnauthorized()
        {
            const string wrongPassword = "wrongpaassword";

            var response = await MakeRequest(UserName, wrongPassword);
            Assert.AreEqual((int)HttpStatusCode.Unauthorized, response.StatusCode, "Status Code Unauthorized");
        }

        [Test]
        public async Task RequestWithNoAuthorizationHeader_ReturnsUnauthorized()
        {
            var response = await MakeRequest(null, null);
            Assert.AreEqual((int)HttpStatusCode.Unauthorized, response.StatusCode, "Status Code Unauthorized");
        }

        private Task<TestHttpResponse> MakeRequest(string userName, string password)
        {
            var request = new TestHttpRequest(WebServerUrl);

            if (userName == null) return SendAsync(request);

            var encodedCredentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{userName}:{password}"));
            var authHeaderValue = new System.Net.Http.Headers.AuthenticationHeaderValue("basic", encodedCredentials);
            request.Headers.Add("Authorization", authHeaderValue.ToString());

            return SendAsync(request);
        }
    }
}