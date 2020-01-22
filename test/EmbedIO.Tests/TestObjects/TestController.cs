﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using EmbedIO.Routing;
using EmbedIO.Utilities;
using EmbedIO.WebApi;

namespace EmbedIO.Tests.TestObjects
{
    public class TestController : WebApiController
    {
        public const string EchoPath = "echo";
        public const string QueryTestPath = "testQuery";
        public const string QueryFieldTestPath = "testQueryField";

        [Route(HttpVerbs.Get, "/empty")]
        public void GetEmpty()
        {
        }

        [Route(HttpVerbs.Get, "/regex")]
        public List<Person> GetPeople() => PeopleRepository.Database;

        [Route(HttpVerbs.Post, "/regex")]
        public Person PostPeople([JsonData] Person person) => person;

        [Route(HttpVerbs.Get, "/regex/{id}")]
        public Person GetPerson(int id) => CheckPerson(id);

        [Route(HttpVerbs.Get, "/regexopt/{id?}")]
        public object GetPerson(int? id)
            => id.HasValue ? (object)CheckPerson(id.Value) : PeopleRepository.Database;

        [Route(HttpVerbs.Get, "/regexdate/{date}")]
        public Person GetPerson(DateTime date)
            => PeopleRepository.Database.FirstOrDefault(p => p.DoB == date)
            ?? throw HttpException.NotFound();

        [Route(HttpVerbs.Get, "/regextwo/{skill}/{age}")]
        public Person GetPerson(string skill, int age)
            => PeopleRepository.Database.FirstOrDefault(p => string.Equals(p.MainSkill, skill, StringComparison.CurrentCultureIgnoreCase) && p.Age == age)
            ?? throw HttpException.NotFound();

        [Route(HttpVerbs.Get, "/regexthree/{skill}/{age?}")]
        public Person GetOptionalPerson(string skill, int? age = null)
        {
            var item = age == null
                ? PeopleRepository.Database.FirstOrDefault(p => string.Equals(p.MainSkill, skill, StringComparison.CurrentCultureIgnoreCase))
                : PeopleRepository.Database.FirstOrDefault(p => string.Equals(p.MainSkill, skill, StringComparison.CurrentCultureIgnoreCase) && p.Age == age);

            return item ?? throw HttpException.NotFound();
        }

        [Route(HttpVerbs.Post, "/" + EchoPath)]
        public Dictionary<string, object?> PostEcho([FormData] NameValueCollection data)
            => data.ToDictionary();

        [Route(HttpVerbs.Get, "/" + QueryTestPath)]
        public Dictionary<string, object?> TestQuery([QueryData] NameValueCollection data)
            => data.ToDictionary();

        [Route(HttpVerbs.Get, "/" + QueryFieldTestPath)]
        public string TestQueryField([QueryField] string id) => id;

        [Route(HttpVerbs.Get, "/notFound")]
        public void GetNotFound() =>
            throw HttpException.NotFound();

        [Route(HttpVerbs.Get, "/unauthorized")]
        public void GetUnauthorized() =>
            throw HttpException.Unauthorized();
        
        [Route(HttpVerbs.Get, "/exception")]
        public void GetException() => throw new Exception("This is an exception");

        [BaseRoute(HttpVerbs.Get, "/testBaseRoute/")]
        public string? TestBaseRoute() => Route.SubPath;

        private static Person CheckPerson(int id)
            => PeopleRepository.Database.FirstOrDefault(p => p.Key == id)
            ?? throw HttpException.NotFound();
    }
}