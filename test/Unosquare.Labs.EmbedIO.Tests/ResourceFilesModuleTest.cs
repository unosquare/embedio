namespace Unosquare.Labs.EmbedIO.Tests
{
    using Constants;
    using Modules;
    using NUnit.Framework;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using TestObjects;

    [TestFixture]
    public class ResourceFilesModuleTest : FixtureBase
    {
        public ResourceFilesModuleTest()
            : base(
                ws =>
                {
                    ws.RegisterModule(new ResourceFilesModule(typeof(ResourceFilesModuleTest).Assembly,
                        "Unosquare.Labs.EmbedIO.Tests.Resources"));
                }, RoutingStrategy.Wildcard)
        {
        }

        [Test]
        public async Task GetIndexFile_ReturnsValidContentFromResource()
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl);

                using (var response = await client.SendAsync(request))
                {
                    Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                    var html = await response.Content.ReadAsStringAsync();

                    Assert.AreEqual(Resources.Index, html, "Same content index.html");
                }
            }
        }

        [Test]
        public async Task GetSubfolderIndexFile_ReturnsValidContentFromResource()
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, WebServerUrl + "sub/index.html");

                using (var response = await client.SendAsync(request))
                {
                    Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");

                    var html = await response.Content.ReadAsStringAsync();

                    Assert.AreEqual(Resources.SubIndex, html, "Same content index.html");
                }
            }
        }
    }
}