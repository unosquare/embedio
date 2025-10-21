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

        private const string CLIENT_ONE_CERT_HASH = "7b49b5d24ea5ae856572fd4f103ffbc4443399ce";
        private const string CLIENT_TWO_CERT_HASH = "a463dcf86fb1daac02a86cda5f9fea7579b8fffb";
        private const string SERVER_CERT_HASH = "435bb9b0dd3b167c9db218572a6817d01b86f45b";

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
        
        /// <summary>
        /// Test server with enabled mutual TLS authentication for certificate acceptance on both sides
        /// </summary>
        [Test]
        [Platform("Win")]
        public async Task OpenWebServerHttpsWithClientCertificate_AcceptsKnownCertificate()
        {
            var options = new WebServerOptions()
                .WithUrlPrefix(HttpsUrl)
                .WithCertificate(new X509Certificate2(@".\ssl\server.pfx", "embedio"))
                .WithMode(HttpListenerMode.EmbedIO)
                .WithClientCertificateValidation(
                    (sender, certificate, chain, errors) => 
                        certificate == null || CLIENT_ONE_CERT_HASH.Equals(certificate.GetCertHashString(), StringComparison.OrdinalIgnoreCase));

            using var webServer = new WebServer(options);
            webServer.OnAny(ctx => {
                Assert.True(ctx.Request.IsAuthenticated, "User is authenticated");
                return ctx.SendStringAsync(DefaultMessage, MimeType.PlainText, WebServer.DefaultEncoding);
            });

            _ = webServer.RunAsync();

            using var httpClientHandler = new HttpClientHandler {
                ServerCertificateCustomValidationCallback = ValidateFixedServerCertificate,
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ClientCertificates = { new X509Certificate2(@"ssl/client1.pfx", "embedio") }
            };
            using var httpClient = new HttpClient(httpClientHandler);
            Assert.AreEqual(DefaultMessage, await httpClient.GetStringAsync(HttpsUrl));
        }
        
        /// <summary>
        /// Test server with enabled mutual TLS authentication during a refused mutual authentication on the client side 
        /// </summary>
        [Test]
        [Platform("Win")]
        public async Task OpenWebServerHttpsWithClientCertificate_CanAcceptAnon()
        {
            var options = new WebServerOptions()
                .WithUrlPrefix(HttpsUrl)
                .WithCertificate(new X509Certificate2(@".\ssl\server.pfx", "embedio"))
                .WithMode(HttpListenerMode.EmbedIO)
                // by treating an missing certificate as okay, we can allow anon requests
                .WithClientCertificateValidation(
                    (sender, certificate, chain, errors) => 
                        certificate == null || CLIENT_ONE_CERT_HASH.Equals(certificate.GetCertHashString(), StringComparison.OrdinalIgnoreCase));

            using var webServer = new WebServer(options);
            webServer.OnAny(ctx => {
                // The user did not provide any certificate and thus is treated as anonymous
                Assert.False(ctx.Request.IsAuthenticated, "User is authenticated");
                return ctx.SendStringAsync(DefaultMessage, MimeType.PlainText, WebServer.DefaultEncoding);
            });

            _ = webServer.RunAsync();

            using var httpClientHandler = new HttpClientHandler {
                ServerCertificateCustomValidationCallback = ValidateFixedServerCertificate
            };
            using var httpClient = new HttpClient(httpClientHandler);
            Assert.AreEqual(DefaultMessage, await httpClient.GetStringAsync(HttpsUrl));
        }
        
        /// <summary>
        /// Test server with enabled mutual TLS authentication when the provided client certificate is not accepted
        /// </summary>
        [Test]
        [Platform("Win")]
        public async Task OpenWebServerHttpsWithClientCertificate_RejectsUnknownCert()
        {
            var options = new WebServerOptions()
                .WithUrlPrefix(HttpsUrl)
                .WithCertificate(new X509Certificate2(@".\ssl\server.pfx", "embedio"))
                .WithMode(HttpListenerMode.EmbedIO)
                // refuse all certificates to make client certificate validation fail
                .WithClientCertificateValidation(
                    (sender, certificate, chain, errors) => false);

            using var webServer = new WebServer(options);
            webServer.OnAny(ctx => {
                // The user did not provide any certificate and thus is treated as anonymous
                Assert.Fail("Server should refuse service");
                return ctx.SendStringAsync(DefaultMessage, MimeType.PlainText, WebServer.DefaultEncoding);
            });

            _ = webServer.RunAsync();

            using var httpClientHandler = new HttpClientHandler {
                ServerCertificateCustomValidationCallback = ValidateFixedServerCertificate,
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ClientCertificates = { new X509Certificate2(@"ssl/client2.pfx", "embedio") }
            };
            using var httpClient = new HttpClient(httpClientHandler);
            Assert.ThrowsAsync<HttpRequestException>(async () => await httpClient.GetStringAsync(HttpsUrl));
        }

        // Bypass certificate validation.
        private static bool ValidateCertificate(object sender,
                                                X509Certificate certificate,
                                                X509Chain chain,
                                                SslPolicyErrors sslPolicyErrors)
            => true;

        private static bool ValidateFixedServerCertificate(HttpRequestMessage message, X509Certificate2? certificate, X509Chain? chain, SslPolicyErrors errors)
        {
            return certificate != null && SERVER_CERT_HASH.Equals(certificate.GetCertHashString(), StringComparison.OrdinalIgnoreCase);
        }
    }
}