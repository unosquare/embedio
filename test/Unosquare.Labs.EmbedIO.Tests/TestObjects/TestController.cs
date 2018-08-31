namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
{
    using Constants;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Modules;

    public class TestController : WebApiController
    {
        public const string RelativePath = "api/";
        public const string EchoPath = RelativePath + "echo/";
        public const string GetPath = RelativePath + "people/";
        public const string GetAsyncPath = RelativePath + "asyncPeople/";
        public const string GetMiddlePath = RelativePath + "person/*/select";

        public TestController(IHttpContext context)
            : base(context)
        {
        }

        [WebApiHandler(HttpVerbs.Get, "/" + GetMiddlePath)]
        public bool GetPerson()
        {
            try
            {
                // read the middle segment
                var segment = Request.Url.Segments.Reverse().Skip(1).First().Replace("/", string.Empty);
                
                return CheckPerson(segment);
            }
            catch (Exception ex)
            {
                return this.JsonExceptionResponse(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/" + GetPath + "*")]
        public bool GetPeople()
        {
            try
            {
                // read the last segment
                var lastSegment = Request.Url.Segments.Last();

                // if it ends with a / means we need to list people
                if (lastSegment.EndsWith("/"))
                    return this.JsonResponse(PeopleRepository.Database);

                return CheckPerson(lastSegment);
            }
            catch (Exception ex)
            {
                return this.JsonExceptionResponse(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Post, "/" + GetPath + "*")]
        public bool PostPeople()
        {
            try
            {
                var content = this.ParseJson<Person>();

                return this.JsonResponse(content);
            }
            catch (Exception ex)
            {
                return this.JsonExceptionResponse(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Post, "/" + EchoPath + "*")]
        public bool PostEcho()
        {
            try
            {
                var content = this.RequestFormDataDictionary();

                return this.JsonResponse(content);
            }
            catch (Exception ex)
            {
                return this.JsonExceptionResponse(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/" + GetAsyncPath + "*")]
        public Task<bool> GetPeopleAsync()
        {
            try
            {
                // read the last segment
                var lastSegment = Request.Url.Segments.Last();

                // if it ends with a / means we need to list people
                if (lastSegment.EndsWith("/"))
                    return this.JsonResponseAsync(PeopleRepository.Database);

                return Task.FromResult(CheckPerson(lastSegment));
            }
            catch (Exception ex)
            {
                return this.JsonExceptionResponseAsync(ex);
            }
        }
        
        private bool CheckPerson(string personKey)
        {
            if (int.TryParse(personKey, out var key) && PeopleRepository.Database.Any(p => p.Key == key))
            {
                return this.JsonResponse(PeopleRepository.Database.FirstOrDefault(p => p.Key == key));
            }

            throw new KeyNotFoundException($"Key Not Found: {personKey}");
        }
    }

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
        public bool GetName()
        {
            this.NoCache();
            return this.JsonResponse(WebName);
        }

        [WebApiHandler(HttpVerbs.Get, "/namePublic")]
        public bool GetNamePublic()
        {
            Response.AddHeader("Cache-Control", "public");
            return this.JsonResponse(WebName);
        }

        public override void SetDefaultHeaders()
        {
            // do nothing with cache
            Response.AddHeader(CustomHeader, WebName);
        }
    }
}