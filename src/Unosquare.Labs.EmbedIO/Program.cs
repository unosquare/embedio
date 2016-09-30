using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO.Log;
using Unosquare.Labs.EmbedIO.Modules;

namespace Unosquare.Labs.EmbedIO
{
    public class Person
    {
        public int Key { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public DateTime DoB { get; set; }
        public string EmailAddress { get; set; }
        public string PhotoUrl { get; set; }
        public string MainSkill { get; set; }
    }

    public static class PeopleRepository
    {
        public static List<Person> Database = new List<Person>
        {
            new Person()
            {
                Key = 1,
                Name = "Mario Di Vece",
                Age = 31,
                EmailAddress = "mario@unosquare.com",
                DoB = new DateTime(1980, 1, 1),
                MainSkill = "CSharp"
            },
            new Person()
            {
                Key = 2,
                Name = "Geovanni Perez",
                Age = 32,
                EmailAddress = "geovanni.perez@unosquare.com",
                DoB = new DateTime(1980, 2, 2),
                MainSkill = "Javascript"
            },
            new Person()
            {
                Key = 3,
                Name = "Luis Gonzalez",
                Age = 29,
                EmailAddress = "luis.gonzalez@unosquare.com",
                DoB = new DateTime(1980, 3, 3),
                MainSkill = "PHP"
            },
        };
    }


    public class SampleWebApi : WebApiController
    {
        public const string RelativePath = "api/";
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
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
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
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return context.JsonResponse(ex);
            }
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var url = "http://localhost:9696/";

            // Our web server is disposable. Note that if you don't want to use logging,
            // there are alternate constructors that allow you to skip specifying an ILog object.
            using (var server = new WebServer(url, new SimpleConsoleLog()))
            {
                // First, we will configure our web server by adding Modules.
                // Please note that order DOES matter.
                // ================================================================================================
                // If we want to enable sessions, we simply register the LocalSessionModule
                // Beware that this is an in-memory session storage mechanism so, avoid storing very large objects.
                // You can use the server.GetSession() method to get the SessionInfo object and manupulate it.
                server.RegisterModule(new Modules.LocalSessionModule());

                // Set the CORS Rules
                server.RegisterModule(new Modules.CorsModule(
                    // Origins, separated by comma without last slash
                    "http://client.cors-api.appspot.com,http://unosquare.github.io,http://run.plnkr.co",
                    // Allowed headers
                    "content-type, accept",
                    // Allowed methods
                    "post"));

                server.RegisterModule(new StaticFilesModule(@"c:\temp"));
                // The static files module will cache small files in ram until it detects they have been modified.
                server.Module<StaticFilesModule>().UseRamCache = false;
                server.Module<StaticFilesModule>().DefaultExtension = ".html";

                server.RegisterModule(new WebApiModule());
                server.Module<WebApiModule>().RegisterController<SampleWebApi>();

                server.RunAsync();

                // Wait for any key to be pressed before disposing of our web server.
                // In a service we'd manage the lifecycle of of our web server using
                // something like a BackgroundWorker or a ManualResetEvent.
                Console.ReadKey(true);
            }
        }
    }
}