using System;
using System.Net;
using System.Threading.Tasks;
using EmbedIO.Utilities;
using NUnit.Framework;
using Swan;

namespace EmbedIO.Tests
{
    public class ExceptionHandlingTest : EndToEndFixtureBase
    {
        private const HttpStatusCode HttpExceptionStatusCode = HttpStatusCode.GatewayTimeout;

        private readonly string _exceptionMessage = Guid.NewGuid().ToString();
        private readonly string _secondLevelExceptionMessage = Guid.NewGuid().ToString();

        public class Unhandled_FirstLevel : ExceptionHandlingTest
        {
            protected override void OnSetUp()
            {
                Server
                    .OnAny(_ => throw new Exception(_exceptionMessage))
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
                    new[] { _exceptionMessage },
                    response.Headers.GetValues(ExceptionHandler.ExceptionMessageHeaderName));
            }
        }

        public class Unhandled_SecondLevel : ExceptionHandlingTest
        {
            protected override void OnSetUp()
            {
                Server
                    .OnAny(_ => throw new Exception(_exceptionMessage))
                    .HandleUnhandledException((ctx, ex) => throw new Exception(_secondLevelExceptionMessage));
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
                    .OnAny(_ => throw new HttpException(HttpExceptionStatusCode, _exceptionMessage))
                    .HandleHttpException(HttpExceptionHandler.PlainTextResponse);
            }

            [Test]
            public async Task HttpException_ResponseIsAsExpected()
            {
                var response = await Client.GetAsync(UrlPath.Root);

                Assert.IsNotNull(response);
                Assert.AreEqual(HttpExceptionStatusCode, response.StatusCode);
                Assert.AreEqual(
                    _exceptionMessage,
                    await response.Content.ReadAsStringAsync());
            }

            public class Http_SecondLevel : ExceptionHandlingTest
            {
                protected override void OnSetUp()
                {
                    Server
                        .OnAny(_ => throw new HttpException(HttpExceptionStatusCode, _exceptionMessage))
                        .HandleUnhandledException((ctx, ex) => throw new Exception(_secondLevelExceptionMessage));
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