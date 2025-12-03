using EmbedIO.Net;
using NUnit.Framework;
using System;
using System.Reflection;

namespace EmbedIO.Tests.Utilities
{
    public static class HttpListenerRequestCookieExtensions
    {
        public static CookieList ParseCookies(this string cookieHeader)
        {
            var type = Type.GetType("EmbedIO.Net.Internal.HttpListenerRequest, EmbedIO");
            Assert.NotNull(type, "Could not find type EmbedIO.Net.Internal.HttpListenerRequest in assembly EmbedIO");

            var method = type.GetMethod("ParseCookies", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(method, "Could not find static method ParseCookies");

            var result = method.Invoke(null, new object[] { cookieHeader });
            return (CookieList)result;
        }
    }

    [TestFixture]
    public class HttpListenerRequestCookieTest
    {
        [Test]
        public void ParseCookies_SimpleHeader_CreatesCookies()
        {
            var cookies = "a=b; c=d".ParseCookies();
            Assert.AreEqual(2, cookies.Count);
            Assert.AreEqual("a", cookies[0].Name);
            Assert.AreEqual("b", cookies[0].Value);
            Assert.AreEqual("c", cookies[1].Name);
            Assert.AreEqual("d", cookies[1].Value);
        }

        [Test]
        public void ParseCookies_VersionAndAttributes_AppliesToFollowingCookies()
        {
            var cookies = "$Version=1; a=b; $Path=/; $Domain=example.com; c=d".ParseCookies();
            Assert.AreEqual(2, cookies.Count);

            Assert.AreEqual(1, cookies[0].Version);
            Assert.AreEqual("/", cookies[0].Path);
            Assert.AreEqual("example.com", cookies[0].Domain);

            Assert.AreEqual(1, cookies[1].Version);
            // $Path/$Domain apply only when encountered after a cookie; in this header they follow cookie a, so they apply to a only.
            // Depending on ParseCookies implementation, attributes after cookie may apply to the previous cookie. Ensure at least cookie names/values parsed.
            Assert.AreEqual("c", cookies[1].Name);
            Assert.AreEqual("d", cookies[1].Value);
        }

        [Test]
        public void ParseCookies_NameOnlyCookie_CreatesEmptyValue()
        {
            var cookies = "flag".ParseCookies();
            Assert.AreEqual(1, cookies.Count);
            Assert.AreEqual("flag", cookies[0].Name);
            Assert.AreEqual(string.Empty, cookies[0].Value);
        }

        [Test]
        public void ParseCookies_QuotedValue_PreservesQuotes()
        {
            var cookies = "a=\"b,c\"; d=unquoted".ParseCookies();
            Assert.AreEqual(2, cookies.Count);
            Assert.AreEqual("\"b,c\"", cookies[0].Value);
            Assert.AreEqual("unquoted", cookies[1].Value);
        }

        [Test]
        public void ParseCookies_VersionBeforeCookies_AppliesToAllFollowing()
        {
            var cookies = "$Version=2; x=1; y=2".ParseCookies();
            Assert.AreEqual(2, cookies.Count);
            Assert.AreEqual(2, cookies[0].Version);
            Assert.AreEqual("x", cookies[0].Name);
            Assert.AreEqual("1", cookies[0].Value);
            Assert.AreEqual(2, cookies[1].Version);
            Assert.AreEqual("y", cookies[1].Name);
            Assert.AreEqual("2", cookies[1].Value);
        }

        [Test]
        public void ParseCookies_PathAppliesToCurrentCookieOnly()
        {
            var cookies = "a=b; $Path=/x; c=d; $Path=/y; e=f".ParseCookies();
            Assert.AreEqual(3, cookies.Count);
            Assert.AreEqual("a", cookies[0].Name);
            Assert.AreEqual("/x", cookies[0].Path);

            Assert.AreEqual("c", cookies[1].Name);
            Assert.AreEqual("/y", cookies[1].Path);

            Assert.AreEqual("e", cookies[2].Name);
            Assert.IsTrue(string.IsNullOrEmpty(cookies[2].Path));
        }

        [Test]
        public void ParseCookies_PortAttribute_SetsPortOnCurrent()
        {
            var cookies = "a=b; $Port=8080; c=d".ParseCookies();
            Assert.AreEqual(2, cookies.Count);
            Assert.AreEqual("\"8080\"", cookies[0].Port);
            Assert.AreEqual("c", cookies[1].Name);
            Assert.AreEqual("d", cookies[1].Value);
        }

        [Test]
        public void ParseCookies_HandlesExtraSpacesAndTrimming()
        {
            var cookies = " a = b ; c= d ".ParseCookies();
            Assert.AreEqual(2, cookies.Count);
            Assert.AreEqual("a", cookies[0].Name);
            Assert.AreEqual("b", cookies[0].Value);
            Assert.AreEqual("c", cookies[1].Name);
            Assert.AreEqual("d", cookies[1].Value);
        }

        [Test]
        public void ParseCookies_NoNameCookie_IsIgnored()
        {
            var cookies = "=value; a=b".ParseCookies();
            Assert.AreEqual(1, cookies.Count, "Cookie with no name should be ignored.");
            Assert.AreEqual("a", cookies[0].Name);
            Assert.AreEqual("b", cookies[0].Value);
        }

        [Test]
        public void ParseCookies_JsonValue_IsPreserved()
        {
            var jsonValue = "{ \"key\": \"some value\" }";
            var cookieString = $"data={jsonValue}";

            var cookies = cookieString.ParseCookies();
            Assert.AreEqual(1, cookies.Count);
            Assert.AreEqual("data", cookies[0].Name);
            Assert.AreEqual(jsonValue, cookies[0].Value);
        }

        [Test]
        public void ParseCookies_QuotedNameWithJsonValue_ParsesCorrectly()
        {
            var cookies = "\" \"; Location={\"country\":\"\",\"city\":\" \"}; id=5584".ParseCookies();
            Assert.AreEqual(3, cookies.Count);

            Assert.AreEqual("\" \"", cookies[0].Name);
            Assert.AreEqual("", cookies[0].Value);

            Assert.AreEqual("Location", cookies[1].Name);
            Assert.AreEqual("{\"country\":\"\",\"city\":\" \"}", cookies[1].Value);

            Assert.AreEqual("id", cookies[2].Name);
            Assert.AreEqual("5584", cookies[2].Value);
        }
    }
}
