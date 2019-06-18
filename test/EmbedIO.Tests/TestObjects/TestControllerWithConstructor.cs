using System.Threading;
using EmbedIO.Routing;
using EmbedIO.WebApi;

namespace EmbedIO.Tests.TestObjects
{
    public class TestControllerWithConstructor : WebApiController
    {
        public const string CustomHeader = "X-Custom";

        public TestControllerWithConstructor(IHttpContext context, CancellationToken cancellationToken, string name = "Test")
            : base(context, cancellationToken)
        {
            WebName = name;
        }

        public string WebName { get; set; }

        [Route(HttpVerbs.Get, "/name")]
        public object GetName()
        {
            Response.DisableCaching();
            Response.ContentType = MimeType.PlainText;
            return WebName;
        }

        [Route(HttpVerbs.Get, "/namePublic")]
        public object GetNamePublic()
        {
            Response.Headers.Set("Cache-Control", "public");
            Response.ContentType = MimeType.PlainText;
            return WebName;
        }

        protected override void OnBeforeHandler() => Response.Headers.Set(CustomHeader, WebName);
    }
}