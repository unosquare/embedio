using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO.Modules;
using Unosquare.Net;
using static Unosquare.Labs.EmbedIO.IoT.UsersTest;

namespace Unosquare.Labs.EmbedIO.IoT
{    
    class ApiController : BaseWebApiController
    {
        private User _user = new User();

        [WebApiHandler(HttpVerbs.Get, "/api/values")]
        public bool GetValues(WebServer webserver, HttpListenerContext context)
        {
            var values = new[] { "value 1", "value 2" };

            return context.JsonResponse(values);

        }

        [WebApiHandler(HttpVerbs.Get, "/api/user")]
        public bool GetUser(WebServer webserver, HttpListenerContext context)
        {
            return context.JsonResponse(_user.GetUser());
        }

        [WebApiHandler(HttpVerbs.Get, "/api/users")]
        public bool GetUsers(WebServer webserver, HttpListenerContext context)
        {
            return context.JsonResponse(_user.GetUsers());
        }

        [WebApiHandler(HttpVerbs.Post, "/api/saveuser")]
        public bool SaveUser(WebServer server, HttpListenerContext context)
        {
            var user = context.RequestBody();

            return true;
        }
    }
}
