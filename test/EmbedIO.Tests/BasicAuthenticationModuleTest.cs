namespace EmbedIO.Tests
{
    using Constants;
    using Modules;
    using NUnit.Framework;
    using System;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Utilities;

    [TestFixture]
    public class BasicAuthenticationModuleTest : FixtureBase
    {
        private const string UserName = "root";
        private const string Password = "password1234";
        private const string WrongPassword = "wrongpaassword";

        public BasicAuthenticationModuleTest()
            : base(ws =>
                {
                    ws.Modules.Add(new BasicAuthenticationModule("/").WithAccount(UserName, Password));
                },
                RoutingStrategy.Wildcard,
                true)
        {
            // placeholder
        }

        [Test]
        public async Task RequestWithValidCredentials_ReturnsOK()
        {
            using (var response = await MakeRequest(UserName, Password))
            {
                Assert.AreEqual((int)HttpStatusCode.OK, response.StatusCode, "Status Code OK");
            }
        }

        [Test]
        public async Task RequestWithInvalidCredentials_ReturnsUnauthorized()
        {
            using (var response = await MakeRequest(UserName, WrongPassword))
            {
                Assert.AreEqual((int)HttpStatusCode.Unauthorized, response.StatusCode, "Status Code Unauthorized");
            }
        }

        [Test]
        public async Task RequestWithNoAuthorizationHeader_ReturnsUnauthorized()
        {
            using (var response = await MakeRequest(null, null))
            {
                Assert.AreEqual((int)HttpStatusCode.Unauthorized, response.StatusCode, "Status Code Unauthorized");
            }
        }

        private async Task<IHttpResponse> MakeRequest(string userName, string password)
        {
            var request = new TestHttpRequest(WebServerUrl);
            if (userName != null)
            {
                var encodedCredentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{userName}:{password}"));
                var authHeaderValue = new System.Net.Http.Headers.AuthenticationHeaderValue("basic", encodedCredentials);
                request.Headers.Add("Authorization", authHeaderValue.ToString());
            }

            return await SendAsync(request);
        }
    }
}