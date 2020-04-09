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
            [Route(HttpVerbs.Any, "/data/{id}")]
            public Task Id(string id)
                => HttpContext.SendStringAsync(id, MimeType.PlainText, WebServer.DefaultEncoding);

            [Route(HttpVerbs.Any, "/data/{id}/{time?}")]
            public Task Time(string id, string time)
                => HttpContext.SendStringAsync(time, MimeType.PlainText, WebServer.DefaultEncoding);

            [Route(HttpVerbs.Any, "/empty")]
            public void Empty()
            {
            }
        }
    }
}