using System;
using System.Collections.Generic;

namespace EmbedIO.Tests.TestObjects
{
    public static class PeopleRepository
    {
        public static List<Person> Database => new List<Person> {
            new Person {
                Key = 1,
                Name = "Mario Di Vece",
                Age = 31,
                EmailAddress = "mario@unosquare.com",
                DoB = new DateTime(1980, 1, 1),
                MainSkill = "CSharp",
            },
            new Person {
                Key = 2,
                Name = "Geovanni Perez",
                Age = 32,
                EmailAddress = "geovanni.perez@unosquare.com",
                DoB = new DateTime(1980, 2, 2),
                MainSkill = "Javascript",
            },
            new Person {
                Key = 3,
                Name = "Luis Gonzalez",
                Age = 29,
                EmailAddress = "luis.gonzalez@unosquare.com",
                DoB = new DateTime(1980, 3, 3),
                MainSkill = "PHP",
            },
        };
    }

    public class Person
    {
        public int Key { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public DateTime DoB { get; set; }
        public string EmailAddress { get; set; }
        public string MainSkill { get; set; }
    }
}