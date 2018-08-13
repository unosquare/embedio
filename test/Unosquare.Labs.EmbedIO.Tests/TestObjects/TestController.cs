namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
{
    using Constants;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Modules;
#if NET47
    using System.Net;
#else
    using Net;
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
                var segment = context.Request.Url.Segments.Reverse().Skip(1).First().Replace("/", string.Empty);

                // otherwise, we need to parse the key and respond with the entity accordingly
                if (int.TryParse(segment, out var key) && PeopleRepository.Database.Any(p => p.Key == key))
                {
                    return context.JsonResponse(PeopleRepository.Database.FirstOrDefault(p => p.Key == key));
                }

                throw new KeyNotFoundException($"Key Not Found: {segment}");
            }
            catch (Exception ex)
            {
                return context.JsonExceptionResponse(ex);
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
                if (int.TryParse(lastSegment, out var key) && PeopleRepository.Database.Any(p => p.Key == key))
                {
                    return context.JsonResponse(PeopleRepository.Database.FirstOrDefault(p => p.Key == key));
                }

                throw new KeyNotFoundException($"Key Not Found: {lastSegment}");
            }
            catch (Exception ex)
            {
                return context.JsonExceptionResponse(ex);
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
                return context.JsonExceptionResponse(ex);
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
                return context.JsonExceptionResponse(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/" + GetAsyncPath + "*")]
        public Task<bool> GetPeopleAsync(WebServer server, HttpListenerContext context)
        {
            try
            {
                // read the last segment
                var lastSegment = context.Request.Url.Segments.Last();

                // if it ends with a / means we need to list people
                if (lastSegment.EndsWith("/"))
                    return context.JsonResponseAsync(PeopleRepository.Database);

                // otherwise, we need to parse the key and respond with the entity accordingly
                if (int.TryParse(lastSegment, out var key) && PeopleRepository.Database.Any(p => p.Key == key))
                {
                    return context.JsonResponseAsync(PeopleRepository.Database.FirstOrDefault(p => p.Key == key));
                }

                throw new KeyNotFoundException($"Key Not Found: {lastSegment}");
            }
            catch (Exception ex)
            {
                return context.JsonExceptionResponseAsync(ex);
            }
        }
    }

    public class TestControllerWithConstructor : WebApiController
    {
        public const string CustomHeader = "X-Custom";

        public TestControllerWithConstructor(string name = "Test")
        {
            WebName = name;
        }

        public string WebName { get; set; }

        [WebApiHandler(HttpVerbs.Get, "/name")]
        public bool GetName(WebServer server, HttpListenerContext context)
        {
            context.NoCache();
            return context.JsonResponse(WebName);
        }

        [WebApiHandler(HttpVerbs.Get, "/namePublic")]
        public bool GetNamePublic(WebServer server, HttpListenerContext context)
        {
            context.Response.Headers.Add("Cache-Control: public");
            return context.JsonResponse(WebName);
        }

        public override void SetDefaultHeaders(HttpListenerContext context)
        {
            // do nothing with cache
            context.Response.Headers.Add($"{CustomHeader}: {WebName}");
        }
    }
}