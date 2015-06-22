namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using Unosquare.Labs.EmbedIO.Modules;

    public class TestController : WebApiController
    {
        // TODO: Test Async mode

        public class Person
        {
            public int Key { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }
            public string EmailAddress { get; set; }
            public string PhotoUrl { get; set; }
        }

        public const string RelativePath = "api/";
        public const string GetPath = RelativePath + "people/";

        public static List<Person> People = new List<Person>
        {
            new Person() {Key = 1, Name = "Mario Di Vece", Age = 31, EmailAddress = "mario@unosquare.com"},
            new Person() {Key = 2, Name = "Geovanni Perez", Age = 32, EmailAddress = "geovanni.perez@unosquare.com"},
            new Person() {Key = 3, Name = "Luis Gonzalez", Age = 29, EmailAddress = "luis.gonzalez@unosquare.com"},
        };

        [WebApiHandler(HttpVerbs.Get, "/" + GetPath + "*")]
        public bool GetPeople(WebServer server, HttpListenerContext context)
        {
            try
            {
                // read the last segment
                var lastSegment = context.Request.Url.Segments.Last();

                // if it ends with a / means we need to list people
                if (lastSegment.EndsWith("/"))
                    return context.JsonResponse(People);

                // otherwise, we need to parse the key and respond with the entity accordingly
                int key;

                if (int.TryParse(lastSegment, out key) && People.Any(p => p.Key == key))
                {
                    return context.JsonResponse(People.FirstOrDefault(p => p.Key == key));
                }

                throw new KeyNotFoundException("Key Not Found: " + lastSegment);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
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
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
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
