using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Unosquare.Labs.EmbedIO.Modules;

namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
{
    public class TestRegexController : WebApiController
    {
        public const string RelativePath = "api/";

        [WebApiHandler(HttpVerbs.Get, "/" + RelativePath + "regex/{id}")]
        public bool GetPerson(WebServer server, HttpListenerContext context, int id)
        {
            try
            {
                if (PeopleRepository.Database.Any(p => p.Key == id))
                {
                    return context.JsonResponse(PeopleRepository.Database.FirstOrDefault(p => p.Key == id));
                }

                throw new KeyNotFoundException("Key Not Found: " + id);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return context.JsonResponse(ex);
            }
        }
    }
}
