using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Unosquare.Swan;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class HttpsTest
    {
        private const string DefaultMessage = "HOLA";
        private const string HttpsUrl = "https://localhost:5555";

        [Test]
        public async Task OpenWebServerHttps_RetrievesIndex()
        {
            if (Runtime.OS != Unosquare.Swan.OperatingSystem.Windows)
                Assert.Ignore("Only Windows");

            ServicePointManager.ServerCertificateValidationCallback = ValidateCertificate;

            var options = new WebServerOptions()
                .WithUrlPrefix(HttpsUrl)
                .WithAutoLoadCertificate()
                .WithMode(HttpListenerMode.EmbedIO);

            using (var webServer = new WebServer(options))
            {
                webServer.OnAny(ctx => ctx.SendStringAsync(DefaultMessage, MimeType.PlainText, Encoding.UTF8));

                var dump = webServer.RunAsync();

                using (var httpClientHandler = new HttpClientHandler())
                {
                    httpClientHandler.ServerCertificateCustomValidationCallback = ValidateCertificate;
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
            if (Runtime.OS == Unosquare.Swan.OperatingSystem.Windows)
                Assert.Ignore("Ignore Windows");
            
            Assert.Throws<PlatformNotSupportedException>(() => {
                var options = new WebServerOptions()
                    .WithUrlPrefix(HttpsUrl)
                    .WithAutoLoadCertificate();
                
                var server = new WebServer(options);
            });
        }

        [Test]
        public void OpenWebServerHttpsWithoutCert_ThrowsInvalidOperation()
        {
            if (Runtime.OS != Unosquare.Swan.OperatingSystem.Windows)
                Assert.Ignore("Only Windows");

            var options = new WebServerOptions()
                .WithUrlPrefix(HttpsUrl)
                .WithAutoRegisterCertificate();

            Assert.Throws<InvalidOperationException>(() => new WebServer(options));
        }

        [Test]
        public void OpenWebServerHttpsWithInvalidStore_ThrowsInvalidOperation()
        {
            if (Runtime.OS != Unosquare.Swan.OperatingSystem.Windows)
                Assert.Ignore("Only Windows");

            var options = new WebServerOptions()
                .WithUrlPrefix(HttpsUrl)
                .WithCertificate(new X509Certificate2())
                .WithAutoRegisterCertificate();

            Assert.Throws<System.Security.Cryptography.CryptographicException>(() => new WebServer(options));
        }

        // Bypass certificate validation.
        private static bool ValidateCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
            => true;
    }
}