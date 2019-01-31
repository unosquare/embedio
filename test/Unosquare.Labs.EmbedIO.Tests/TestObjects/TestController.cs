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
        public const string GetMiddlePath = RelativePath + "person/*/select";

        public TestController(IHttpContext context)
            : base(context)
        {
        }

        [WebApiHandler(HttpVerbs.Get, "/" + GetMiddlePath)]
        public Task<bool> GetPerson()
        {
            try
            {
                // read the middle segment
                var segment = Request.Url.Segments.Reverse().Skip(1)
                    .First()
                    .Replace("/", string.Empty);

                return CheckPerson(segment);
            }
            catch (Exception ex)
            {
                return this.JsonExceptionResponseAsync(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/" + GetPath + "*")]
        public Task<bool> GetPeople()
        {
            try
            {
                // read the last segment
                var lastSegment = Request.Url.Segments.Last();

                // if it ends with a / means we need to list people
                return lastSegment.EndsWith("/")
                    ? this.JsonResponseAsync(PeopleRepository.Database)
                    : CheckPerson(lastSegment);
            }
            catch (Exception ex)
            {
                return this.JsonExceptionResponseAsync(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Post, "/" + GetPath + "*")]
        public async Task<bool> PostPeople()
        {
            try
            {
                var content = await this.ParseJsonAsync<Person>();

                return await this.JsonResponseAsync(content);
            }
            catch (Exception ex)
            {
                return await this.JsonExceptionResponseAsync(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Post, "/" + EchoPath + "*")]
        public async Task<bool> PostEcho()
        {
            try
            {
                var content = await this.RequestFormDataDictionaryAsync();

                return await this.JsonResponseAsync(content);
            }
            catch (Exception ex)
            {
                return await this.JsonExceptionResponseAsync(ex);
            }
        }
        
        private Task<bool> CheckPerson(string personKey)
        {
            if (int.TryParse(personKey, out var key) && PeopleRepository.Database.Any(p => p.Key == key))
            {
                return this.JsonResponseAsync(PeopleRepository.Database.FirstOrDefault(p => p.Key == key));
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
        public Task<bool> GetName()
        {
            this.NoCache();
            return this.JsonResponseAsync(WebName);
        }

        [WebApiHandler(HttpVerbs.Get, "/namePublic")]
        public Task<bool> GetNamePublic()
        {
            Response.AddHeader("Cache-Control", "public");
            return this.JsonResponseAsync(WebName);
        }

        public override void SetDefaultHeaders()
        {
            // do nothing with cache
            Response.AddHeader(CustomHeader, WebName);
        }
    }
}