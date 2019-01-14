namespace Unosquare.Labs.EmbedIO.Tests
{
    using Swan;
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Net.Http;
    using NUnit.Framework;
    using System.Threading.Tasks;

    [TestFixture]
    public class HttpsTest
    {
        private const string DefaultMessage = "HOLA";
        private const string HttpsUrl = "https://localhost:5555";

        [Test]
        public async Task OpenWebServerHttps_RetrievesIndex()
        {
            if (Runtime.OS != Swan.OperatingSystem.Windows)
                Assert.Ignore("Only Windows");

            // bypass certification validation
            System.Net.ServicePointManager.ServerCertificateValidationCallback = (s, c, cert, x) => true;

            var options = new WebServerOptions(HttpsUrl)
            {
                AutoLoadCertificate = true,
                Mode = HttpListenerMode.EmbedIO,
            };

            using (var webServer = new WebServer(options))
            {
                webServer.OnAny((ctx, ct) => ctx.HtmlResponseAsync(DefaultMessage, cancellationToken: ct));

                webServer.RunAsync();

                using (var httpClientHandler = new HttpClientHandler())
                {
#if !NET452
                    httpClientHandler.ServerCertificateCustomValidationCallback = (s, c, cert, x) => true;
#endif
                    using (var httpClient = new HttpClient(httpClientHandler))
                    {
                        Assert.AreEqual(DefaultMessage, await httpClient.GetStringAsync(HttpsUrl));
                    }
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
                AutoLoadCertificate = true,
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
                Certificate = new X509Certificate2(),
            };

            Assert.Throws<System.Security.Cryptography.CryptographicException>(() => new WebServer(options));
        }
    }
}
