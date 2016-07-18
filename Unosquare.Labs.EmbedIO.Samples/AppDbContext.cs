using Unosquare.Labs.LiteLib;

namespace Unosquare.Labs.EmbedIO.Samples
{
    internal class AppDbContext : LiteDbContext
    {
        public AppDbContext()
            : base("mydbfile.db", null)
        {
            // map this context to the database file mydbfile.db and don't use any logging capabilities.
        }
    }
}
