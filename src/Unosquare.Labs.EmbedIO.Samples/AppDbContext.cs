using Unosquare.Labs.LiteLib;

namespace Unosquare.Labs.EmbedIO.Samples
{
    internal sealed class AppDbContext : LiteDbContext
    {
        public AppDbContext() : base("mydbfile.db", false)
        {
            // map this context to the database file mydbfile.db and don't use any logging capabilities.
        }

        public LiteDbSet<Person> People { get; set; }

        public static void InitDatabase()
        {
            var dbContext = new AppDbContext();

            foreach (var person in dbContext.People.SelectAll())
                dbContext.People.Delete(person);

            dbContext.People.Insert(new Person
            {
                Name = "Mario Di Vece",
                Age = 31,
                EmailAddress = "mario@unosquare.com"
            });
            dbContext.People.Insert(new Person
            {
                Name = "Geovanni Perez",
                Age = 32,
                EmailAddress = "geovanni.perez@unosquare.com"
            });
            dbContext.People.Insert(new Person
            {
                Name = "Luis Gonzalez",
                Age = 29,
                EmailAddress = "luis.gonzalez@unosquare.com"
            });
        }
    }
}