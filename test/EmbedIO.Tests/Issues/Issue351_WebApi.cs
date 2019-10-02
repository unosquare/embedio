using System;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NUnit.Framework;

namespace EmbedIO.Tests.Issues
{
    public class Issue351_WebApi
    {
        [Test]
        public void WebApiModuleBase_RegisterController_WithNoControllerMethods_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new WebApiModule("/")
                .WithController<TestControllerWithNoControllerMethods>());
        }

        [Test]
        public void WebApiControllerParameter_OfReferenceType_WithoutSpecifiedValue_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => new WebApiModule("/")
                .WithController<TestController>());
        }

        private class TestControllerWithNoControllerMethods : WebApiController
        {
        }

        private class TestController : WebApiController
        {
            // Issue 351: registering this controller would throw ArgumentException
            // because the handler compiler would pass (System.Object)null
            // instead of (System.String)null for parameter str.
            [Route(HttpVerbs.Get, "/")]
            public void GiveMeTheDefaultValueForString(string str)
            {
            }
        }
    }
}