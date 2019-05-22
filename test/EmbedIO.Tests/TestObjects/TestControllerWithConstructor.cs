using EmbedIO.Constants;
using EmbedIO.Modules;
using System.Threading.Tasks;

namespace EmbedIO.Tests.TestObjects
{
    public class TestControllerWithConstructor : WebApiController
    {
        public const string CustomHeader = "X-Custom";

        public TestControllerWithConstructor(IHttpContext context, string name = "Test")
            : base(context)
        {
            WebName = name;
        }

        public string WebName { get; set; }

        [WebApiHandler(HttpVerbs.Get, "/name")]
        public Task<bool> GetName()
        {
            Response.NoCache();
            return Ok(WebName);
        }

        [WebApiHandler(HttpVerbs.Get, "/namePublic")]
        public Task<bool> GetNamePublic()
        {
            Response.AddHeader("Cache-Control", "public");
            return Ok(WebName);
        }

        public override void SetDefaultHeaders()
        {
            // do nothing with cache
            Response.AddHeader(CustomHeader, WebName);
        }
    }
}