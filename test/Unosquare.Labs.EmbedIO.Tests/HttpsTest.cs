namespace Unosquare.Labs.EmbedIO.Tests
{
    using Unosquare.Swan;
    using System.Net.Http;
    using TestObjects;
    using NUnit.Framework;
    using System.Threading.Tasks;

    [TestFixture]
    public class HttpsTest
    {
        [Test]
        public async Task OpenWebServerHttps_RetrievesIndex()
        {
            if (Runtime.OS != OperatingSystem.Windows)
                Assert.Ignore("Only Windows");

            var options = new WebServerOptions("https://localhost:5555")
            {
                AutoRegisterCertificate = true,
                Certificate = CertificateHelper.CreateOrLoadCertificate("temp.pfx", "localhost", "MyPassword"),
            };

            using (var webServer = new WebServer(options))
            {
                webServer.OnAny((ctx, ct) => ctx.HtmlResponseAsync("HOLA", cancellationToken: ct));

                webServer.RunAsync();

                using (var httpClient = new HttpClient())
                {
                    Assert.AreEqual("HOLA", await httpClient.GetStringAsync("https://localhost:5555"));
                }
            }
        }
    }
}
