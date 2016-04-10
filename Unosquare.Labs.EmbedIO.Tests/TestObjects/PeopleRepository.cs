using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
{
    public class Person
    {
        public int Key { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string EmailAddress { get; set; }
        public string PhotoUrl { get; set; }
    }

    public static class PeopleRepository
    {
        public static List<Person> Database = new List<Person>
        {
            new Person() {Key = 1, Name = "Mario Di Vece", Age = 31, EmailAddress = "mario@unosquare.com"},
            new Person() {Key = 2, Name = "Geovanni Perez", Age = 32, EmailAddress = "geovanni.perez@unosquare.com"},
            new Person() {Key = 3, Name = "Luis Gonzalez", Age = 29, EmailAddress = "luis.gonzalez@unosquare.com"},
        };
    }
}
