using System.Threading;
using System.Threading.Tasks;
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

        [RouteHandler(HttpVerbs.Get, "/name")]
        public Task<bool> GetName()
        {
            Response.DisableCaching();
            return Ok(WebName, MimeTypes.PlainTextType);
        }

        [RouteHandler(HttpVerbs.Get, "/namePublic")]
        public Task<bool> GetNamePublic()
        {
            Response.Headers.Set("Cache-Control", "public");
            return Ok(WebName, MimeTypes.PlainTextType);
        }

        protected override void OnBeforeHandler() => Response.Headers.Set(CustomHeader, WebName);
    }
}