namespace Unosquare.Labs.EmbedIO.Tests
{
    using Swan;
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Net.Http;
    using TestObjects;
    using NUnit.Framework;
    using System.Threading.Tasks;

    [TestFixture]
    public class HttpsTest
    {
        private const string DefaultMessage = "HOLA";
        private const string HttpsUrl = "https://localhost:5555";

        private readonly X509Certificate2 _certificate =
            CertificateHelper.CreateOrLoadCertificate("temp.pfx", "localhost");

        [Test]
        public async Task OpenWebServerHttps_RetrievesIndex()
        {
            if (Runtime.OS != Swan.OperatingSystem.Windows)
                Assert.Ignore("Only Windows");

            var options = new WebServerOptions(HttpsUrl)
            {
                AutoRegisterCertificate = true,
                Certificate = _certificate,
            };

            using (var webServer = new WebServer(options))
            {
                webServer.OnAny((ctx, ct) => ctx.HtmlResponseAsync(DefaultMessage, cancellationToken: ct));

                webServer.RunAsync();

                using (var httpClient = new HttpClient())
                {
                    Assert.AreEqual(DefaultMessage, await httpClient.GetStringAsync(HttpsUrl));
                }
            }
        }

        [Test]
        public void OpenWebServerHttpsWithLinuxOrMac_ThrowsInvalidOperation()
        {
            if (Runtime.OS == Swan.OperatingSystem.Windows)
                Assert.Ignore("Ignore Windows");

            var options = new WebServerOptions(HttpsUrl)
            {
                AutoRegisterCertificate = true,
                Certificate = _certificate,
            };

            Assert.Throws<InvalidOperationException>(() => new WebServer(options));
        }

        [Test]
        public void OpenWebServerHttpsWithoutCert_ThrowsInvalidOperation()
        {
            if (Runtime.OS != Swan.OperatingSystem.Windows)
                Assert.Ignore("Only Windows");

            var options = new WebServerOptions(HttpsUrl)
            {
                AutoRegisterCertificate = true,
            };

            Assert.Throws<InvalidOperationException>(() => new WebServer(options));
        }

        [Test]
        public void OpenWebServerHttpsWithInvalidStore_ThrowsInvalidOperation()
        {
            if (Runtime.OS != Swan.OperatingSystem.Windows)
                Assert.Ignore("Only Windows");

            var options = new WebServerOptions(HttpsUrl)
            {
                AutoRegisterCertificate = true,
                Certificate = _certificate,
            };

            Assert.Throws<InvalidOperationException>(() => new WebServer(options));
        }
    }
}
