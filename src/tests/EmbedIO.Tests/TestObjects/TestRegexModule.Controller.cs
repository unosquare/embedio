using System.Text;
using System.Threading.Tasks;
using EmbedIO.Routing;
using EmbedIO.WebApi;

namespace EmbedIO.Tests.TestObjects
{
    partial class TestRegexModule
    {
        public class Controller : WebApiController
        {
            [Route(HttpVerb.Any, "/data/{id}")]
            public Task Id(string id)
                => HttpContext.SendStringAsync(id, MimeType.PlainText, Encoding.UTF8);

            [Route(HttpVerb.Any, "/data/{id}/{time?}")]
            public Task Time(string id, string time)
                => HttpContext.SendStringAsync(time, MimeType.PlainText, Encoding.UTF8);

            [Route(HttpVerb.Any, "/empty")]
            public void Empty()
            {
            }
        }
    }
}