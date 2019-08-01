using Unosquare.Labs.LiteLib;
using Swan;

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

#pragma warning disable 0618 // "Use a better hasher." - Not our fault if gravatar.com uses MD5.
        public string PhotoUrl => $"http://www.gravatar.com/avatar/{EmailAddress.ComputeMD5().ToUpperHex()}.png?s=100";
#pragma warning restore 0618
    }
}