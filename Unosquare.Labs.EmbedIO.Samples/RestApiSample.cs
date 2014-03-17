namespace Unosquare.Labs.EmbedIO.Samples
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using Unosquare.Labs.EmbedIO.Modules;

    public static partial class RestApiSample
    {
        private const string RelativePath = "/api/";
        public static List<Person> People { get; private set; }

        /// <summary>
        /// Here we add the WebApiModule to our Web Server and register our controller classes.
        /// You can register as many controller classes as you would like
        /// We also add some records to our People list
        /// </summary>
        /// <param name="server">The server.</param>
        public static void Setup(WebServer server)
        {
            People = new List<Person>()
            {
                new Person() {Key = 1, Name = "Mario Di Vece", Age = 31},
                new Person() {Key = 2, Name = "Geovanni Perez", Age = 32},
                new Person() {Key = 3, Name = "Luis Gonzalez", Age = 29},
            };

            server.Modules.Add(new WebApiModule());
            server.Module<WebApiModule>().RegisterController<PeopleController>();
        }

        /// <summary>
        /// A simple model representing a person
        /// </summary>
        public class Person
        {
            public int Key { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }
        }

        /// <summary>
        /// A very simple controller to handle People CRUD.
        /// Notice how it Inherits from WebApiController and the methods have WebApiHandler attributes 
        /// This is for sampling purposes only.
        /// </summary>
        public class PeopleController : WebApiController
        {
            /// <summary>
            /// Gets the people.
            /// This will respond to 
            ///     GET http://localhost:9696/api/people/
            ///     GET http://localhost:9696/api/people/1
            ///     GET http://localhost:9696/api/people/{n}
            /// 
            /// Notice the wildcard is important
            /// </summary>
            /// <param name="server">The server.</param>
            /// <param name="context">The context.</param>
            /// <returns></returns>
            /// <exception cref="System.Collections.Generic.KeyNotFoundException">Key Not Found:  + lastSegment</exception>
            [WebApiHandler(HttpVerbs.Get, RelativePath + "people/*")]
            public bool GetPeople(WebServer server, HttpListenerContext context)
            {
                try
                {
                    // read the last segment
                    var lastSegment = context.Request.Url.Segments.Last();

                    // if it ends with a / means we need to list people
                    if (lastSegment.EndsWith("/"))
                        return context.JsonResponse(RestApiSample.People);

                    // otherwise, we need to parse the key and respond with the entity accordingly
                    int key = 0;
                    if (int.TryParse(lastSegment, out key) && People.Any(p => p.Key == key))
                    {
                        return context.JsonResponse(People.FirstOrDefault(p => p.Key == key));
                    }

                    throw new KeyNotFoundException("Key Not Found: " + lastSegment);
                }
                catch (Exception ex)
                {
                    // here the error handler will respond with a generic 500 HTTP code a JSON-encoded object
                    // with error info. You will need to handle HTTP status codes correctly depending on the situation.
                    // For example, for keys that are not found, ou will need to respond with a 404 status code.
                    return HandleError(context, ex, 500);
                }

            }

            /// <summary>
            /// Handles the error returning an error status code and json-encoded body.
            /// </summary>
            /// <param name="context">The context.</param>
            /// <param name="ex">The ex.</param>
            /// <param name="statusCode">The HTTP status code.</param>
            /// <returns></returns>
            protected bool HandleError(HttpListenerContext context, Exception ex, int statusCode = 500)
            {
                var errorResponse = new
                {
                    Title = "Unexpected Error",
                    ErrorCode = ex.GetType().Name,
                    Description = ex.ExceptionMessage(),
                };

                context.Response.StatusCode = statusCode;
                return context.JsonResponse(errorResponse);
            }

        }



    }
}
