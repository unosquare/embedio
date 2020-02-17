using EmbedIO.Routing;
using EmbedIO.WebApi;

namespace EmbedIO.Forms.Sample
{
    public class TestController : WebApiController
    {
        public TestController() : base()
        { }

        [Route(HttpVerbs.Get, "/testresponse")]
        public int GetTestResponse()
        {
            return 12345;
        }
    }
}

