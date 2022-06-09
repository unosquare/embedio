using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using EmbedIO.Tests.TestObjects;
using NUnit.Framework;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class HttpsTest
    {
        private const string DefaultMessage = "HOLA";
        private const string HttpsUrl = "https://localhost:5555";

        [Test]
        [Platform("Win")]
        public async Task OpenWebServerHttps_RetrievesIndex()
        {
            ServicePointManager.ServerCertificateValidationCallback = ValidateCertificate;

            var options = new WebServerOptions()
                .WithUrlPrefix(HttpsUrl)
                .WithAutoLoadCertificate()
                .WithMode(HttpListenerMode.EmbedIO);

            using var webServer = new WebServer(options);
            webServer.OnAny(ctx => ctx.SendStringAsync(DefaultMessage, MimeType.PlainText, WebServer.DefaultEncoding));

            _ = webServer.RunAsync();

            using var httpClientHandler = new HttpClientHandler {ServerCertificateCustomValidationCallback = ValidateCertificate};
            using var httpClient = new HttpClient(httpClientHandler);
            Assert.AreEqual(DefaultMessage, await httpClient.GetStringAsync(HttpsUrl));
        }

        [Test]
        [Platform(Exclude = "Win")]
        public void OpenWebServerHttpsWithLinuxOrMac_ThrowsInvalidOperation()
        {
            Assert.Throws<PlatformNotSupportedException>(() => {
                var options = new WebServerOptions()
                    .WithUrlPrefix(HttpsUrl)
                    .WithAutoLoadCertificate();

                new WebServer(options).Void();
            });
        }

        [Test]
        [Platform("Win")]
        public void OpenWebServerHttpsWithoutCert_ThrowsInvalidOperation()
        {
            var options = new WebServerOptions()
                .WithUrlPrefix(HttpsUrl)
                .WithAutoRegisterCertificate();

            Assert.Throws<InvalidOperationException>(() => new WebServer(options).Void());
        }

        [Test]
        [Platform("Win")]
        public void OpenWebServerHttpsWithInvalidStore_ThrowsInvalidOperation()
        {
            var options = new WebServerOptions()
                .WithUrlPrefix(HttpsUrl)
                .WithCertificate(new X509Certificate2())
                .WithAutoRegisterCertificate();

            Assert.Throws<System.Security.Cryptography.CryptographicException>(() => _ = new WebServer(options));
        }

        // Bypass certificate validation.
        private static bool ValidateCertificate(object sender,
                                                X509Certificate certificate,
                                                X509Chain chain,
                                                SslPolicyErrors sslPolicyErrors)
            => true;
    }
}