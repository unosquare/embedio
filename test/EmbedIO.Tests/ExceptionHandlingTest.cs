using System;
using System.Net;
using System.Threading.Tasks;
using EmbedIO.Utilities;
using NUnit.Framework;
using Unosquare.Swan;

namespace EmbedIO.Tests
{
    public class ExceptionHandlingTest : EndToEndFixtureBase
    {
        const HttpStatusCode HttpExceptionStatusCode = HttpStatusCode.GatewayTimeout;

        private readonly string ExceptionMessage = Guid.NewGuid().ToString();
        private readonly string SecondLevelExceptionMessage = Guid.NewGuid().ToString();

        public ExceptionHandlingTest()
            : base(true)
        {
        }

        public class Unhandled_FirstLevel : ExceptionHandlingTest
        {
            protected override void OnSetUp()
            {
                Server
                    .OnAny((ctx, path, ct) => throw new Exception(ExceptionMessage))
                    .HandleUnhandledException(ExceptionHandler.EmptyResponseWithHeaders);
            }

            [Test]
            public async Task UnhandledException_ResponseIsAsExpected()
            {
                var response = await Client.GetAsync(UrlPath.Root);

                Assert.IsNotNull(response);
                Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
                CollectionAssert.AreEqual(
                    new[] { nameof(Exception) },
                    response.Headers.GetValues(ExceptionHandler.ExceptionTypeHeaderName));

                CollectionAssert.AreEqual(
                    new[] { ExceptionMessage },
                    response.Headers.GetValues(ExceptionHandler.ExceptionMessageHeaderName));
            }
        }

        public class Unhandled_SecondLevel : ExceptionHandlingTest
        {
            protected override void OnSetUp()
            {
                Server
                    .OnAny((ctx, path, ct) => throw new Exception(ExceptionMessage))
                    .HandleUnhandledException((ctx, ex, ct) => throw new Exception(SecondLevelExceptionMessage));
            }

            [Test]
            public void SecondLevelException_ServerDoesNotCrash()
            {
                // When using a TestWebServer, context handling code is called by the client;
                // hence, an unhandled second-level exception would be seen here.
                Assert.DoesNotThrow(() => Client.GetAsync(UrlPath.Root).Await());
            }
        }

        public class Http_FirstLevel : ExceptionHandlingTest
        {
            protected override void OnSetUp()
            {
                Server
                    .OnAny((ctx, path, ct) => throw new HttpException(HttpExceptionStatusCode, ExceptionMessage))
                    .HandleHttpException(HttpExceptionHandler.PlainTextResponse);
            }

            [Test]
            public async Task HttpException_ResponseIsAsExpected()
            {
                var response = await Client.GetAsync(UrlPath.Root);

                Assert.IsNotNull(response);
                Assert.AreEqual(HttpExceptionStatusCode, response.StatusCode);
                Assert.AreEqual(
                    ExceptionMessage,
                    await response.Content.ReadAsStringAsync());
            }

            public class Http_SecondLevel : ExceptionHandlingTest
            {
                protected override void OnSetUp()
                {
                    Server
                        .OnAny((ctx, path, ct) => throw new HttpException(HttpExceptionStatusCode, ExceptionMessage))
                        .HandleUnhandledException((ctx, ex, ct) => throw new Exception(SecondLevelExceptionMessage));
                }

                [Test]
                public void SecondLevelException_ServerDoesNotCrash()
                {
                    // When using a TestWebServer, context handling code is called by the client;
                    // hence, an unhandled second-level exception would be seen here.
                    Assert.DoesNotThrow(() => Client.GetAsync(UrlPath.Root).Await());
                }
            }
        }
    }
}