using System.Collections.Generic;
using System.Threading.Tasks;
using Swan;
using Swan.Cryptography;

namespace EmbedIO.Samples
{
    public class Person
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Age { get; set; }

        public string EmailAddress { get; set; }

#pragma warning disable 0618 // "Use a better hasher." - Not our fault if gravatar.com uses MD5.
        public string PhotoLocation => $"http://www.gravatar.com/avatar/{Hasher.ComputeMD5(EmailAddress).ToUpperHex()}.png?s=100";
#pragma warning restore 0618

        internal static async Task<IEnumerable<Person>> GetDataAsync()
        {
            // Imagine this is a database call :)
            await Task.Delay(0).ConfigureAwait(false);

            return new List<Person>
            {
                new Person
                {
                    Id = 1,
                    Name = "Mario Di Vece",
                    Age = 31,
                    EmailAddress = "mario@unosquare.com",
                },
                new Person
                {
                    Id = 2,
                    Name = "Geovanni Perez",
                    Age = 32,
                    EmailAddress = "geovanni.perez@unosquare.com",
                },
                new Person
                {
                    Id = 3,
                    Name = "Luis Gonzalez",
                    Age = 29,
                    EmailAddress = "luis.gonzalez@unosquare.com",
                },
            };
        }
    }
}