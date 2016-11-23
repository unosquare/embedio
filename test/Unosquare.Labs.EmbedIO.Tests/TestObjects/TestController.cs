namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Unosquare.Labs.EmbedIO.Modules;
#if NET46
    using System.Net;
#else
    using Unosquare.Net;
#endif

    public class TestController : WebApiController
    {
        public const string RelativePath = "api/";
        public const string EchoPath = RelativePath + "echo/";
        public const string GetPath = RelativePath + "people/";
        public const string GetAsyncPath = RelativePath + "asyncPeople/";
        public const string GetMiddlePath = RelativePath + "person/*/select";

        [WebApiHandler(HttpVerbs.Get, "/" + GetMiddlePath)]
        public bool GetPerson(WebServer server, HttpListenerContext context)
        {
            try
            {
                // read the middle segment
                var segment = context.Request.Url.Segments.Reverse().Skip(1).First().Replace("/", "");

                // otherwise, we need to parse the key and respond with the entity accordingly
                int key;

                if (int.TryParse(segment, out key) && PeopleRepository.Database.Any(p => p.Key == key))
                {
                    return context.JsonResponse(PeopleRepository.Database.FirstOrDefault(p => p.Key == key));
                }

                throw new KeyNotFoundException("Key Not Found: " + segment);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                return context.JsonResponse(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/" + GetPath + "*")]
        public bool GetPeople(WebServer server, HttpListenerContext context)
        {
            try
            {
                // read the last segment
                var lastSegment = context.Request.Url.Segments.Last();

                // if it ends with a / means we need to list people
                if (lastSegment.EndsWith("/"))
                    return context.JsonResponse(PeopleRepository.Database);

                // otherwise, we need to parse the key and respond with the entity accordingly
                int key;

                if (int.TryParse(lastSegment, out key) && PeopleRepository.Database.Any(p => p.Key == key))
                {
                    return context.JsonResponse(PeopleRepository.Database.FirstOrDefault(p => p.Key == key));
                }

                throw new KeyNotFoundException("Key Not Found: " + lastSegment);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                return context.JsonResponse(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Post, "/" + GetPath + "*")]
        public bool PostPeople(WebServer server, HttpListenerContext context)
        {
            try
            {
                var content = context.ParseJson<Person>();

                return context.JsonResponse(content);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                return context.JsonResponse(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Post, "/" + EchoPath + "*")]
        public bool PostEcho(WebServer server, HttpListenerContext context)
        {
            try
            {
                var content = context.RequestFormDataDictionary();

                return context.JsonResponse(content);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                return context.JsonResponse(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/" + GetAsyncPath + "*")]
        public async Task<bool> GetPeopleAsync(WebServer server, HttpListenerContext context)
        {
            try
            {
                // sleep because task
                await Task.Delay(TimeSpan.FromSeconds(1));

                // read the last segment
                var lastSegment = context.Request.Url.Segments.Last();

                // if it ends with a / means we need to list people
                if (lastSegment.EndsWith("/"))
                    return context.JsonResponse(PeopleRepository.Database);

                // otherwise, we need to parse the key and respond with the entity accordingly
                int key;

                if (int.TryParse(lastSegment, out key) && PeopleRepository.Database.Any(p => p.Key == key))
                {
                    return context.JsonResponse(PeopleRepository.Database.FirstOrDefault(p => p.Key == key));
                }

                throw new KeyNotFoundException("Key Not Found: " + lastSegment);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                return context.JsonResponse(ex);
            }
        }
    }

    public class TestControllerWithConstructor : WebApiController
    {
        public string WebName { get; set; }

        public TestControllerWithConstructor(string name)
        {
            WebName = name;
        }

        [WebApiHandler(HttpVerbs.Get, "/name")]
        public bool GetPeople(WebServer server, HttpListenerContext context)
        {
            return context.JsonResponse(WebName);
        }
    }
}