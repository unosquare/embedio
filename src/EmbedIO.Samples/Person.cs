using Unosquare.Labs.LiteLib;
using Unosquare.Swan;

namespace EmbedIO.Samples
{
    /// <inheritdoc />
    /// <summary>
    /// A simple model representing a person
    /// </summary>
    [Table("Persons")]
    public class Person : LiteModel
    {
        public string Name { get; set; }
        public int Age { get; set; }

        [LiteIndex]
        public string EmailAddress { get; set; }

        public string PhotoUrl => $"http://www.gravatar.com/avatar/{EmailAddress.ComputeMD5().ToUpperHex()}.png?s=100";
    }
}