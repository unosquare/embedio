using System;
using System.Linq;
using EmbedIO.Routing;
using NUnit.Framework;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class RoutingTest
    {
        [TestCase("")] // Route is empty.
        [TestCase("abc")] // Route does not start with a slash.
        [TestCase("/abc/{id")] // Route syntax error: unclosed parameter specification.
        [TestCase("/abc/{}")] // Route syntax error: empty parameter specification.
        [TestCase("/abc/{?}")] // Route syntax error: missing parameter name.
        [TestCase("/abc/{myp@rameter}")] // Route syntax error: parameter name contains one or more invalid characters.
        [TestCase("/abc/{id}/def/{id}")] // Route syntax error: duplicate parameter name.
        [TestCase("/abc/{id}{name}")] // Route syntax error: parameters must be separated by literal text.
        public void InvalidRoute_IsNotValid(string route)
        {
            RouteMatcher.ClearCache();

            Assert.IsFalse(Route.IsValid(route, false));
            Assert.Throws<FormatException>(() => RouteMatcher.Parse(route, false));
            Assert.IsFalse(RouteMatcher.TryParse(route, false, out _));
        }

        [TestCase("")] // Route is empty.
        [TestCase("abc/")] // Route does not start with a slash.
        [TestCase("/abc/{id/")] // Route syntax error: unclosed parameter specification.
        [TestCase("/abc/{}/")] // Route syntax error: empty parameter specification.
        [TestCase("/abc/{myp@rameter}/")] // Route syntax error: parameter name contains one or more invalid characters.
        [TestCase("/abc/{id}/def/{id}/")] // Route syntax error: duplicate parameter name.
        [TestCase("/abc/{id}{name}/")] // Route syntax error: parameters must be separated by literal text.
        [TestCase("/abc/{id}/{name?}/")] // No segment of a base route can be optional.
        public void InvalidBaseRoute_IsNotValid(string route)
        {
            RouteMatcher.ClearCache();

            Assert.IsFalse(Route.IsValid(route, true));
            Assert.Throws<FormatException>(() => RouteMatcher.Parse(route, true));
            Assert.IsFalse(RouteMatcher.TryParse(route, true, out _));
        }

        [TestCase("/")] // Root.
        [TestCase("/abc/def")] // No parameters.
        [TestCase("/abc/{id}")] // 1 parameter, takes a whole segment.
        [TestCase("/abc/{id?}")] // 1 optional parameter, takes a whole segment.
        [TestCase("/a{id}")] // 1 parameter, at start of segment.
        [TestCase("/{id}b")] // 1 parameter, at end of segment.
        [TestCase("/a{id}b")] // 1 parameter, mid-segment.
        [TestCase("/abc/{width}x{height}")] // 2 parameters, same segment.
        [TestCase("/abc/{width}/{height}")] // 2 parameters, different segments.
        [TestCase("/abc/{id}/{date?}")] // 2 parameters, different segments, 1 optional.
        public void ValidRoute_IsValid(string route)
        {
            RouteMatcher.ClearCache();

            Assert.IsTrue(Route.IsValid(route, false));
            Assert.DoesNotThrow(() => RouteMatcher.Parse(route, false));
            Assert.IsTrue(RouteMatcher.TryParse(route, false, out _));
        }

        [TestCase("/")] // Root.
        [TestCase("/abc/def/")] // No parameters.
        [TestCase("/abc/{id}/")] // 1 parameter, takes a whole segment.
        [TestCase("/a{id}/")] // 1 parameter, at start of segment.
        [TestCase("/{id}b/")] // 1 parameter, at end of segment.
        [TestCase("/a{id}b/")] // 1 parameter, mid-segment.
        [TestCase("/abc/{width}x{height}/")] // 2 parameters, same segment.
        [TestCase("/abc/{width}/{height}/")] // 2 parameters, different segments.
        public void ValidBaseRoute_IsValid(string route)
        {
            RouteMatcher.ClearCache();

            Assert.IsTrue(Route.IsValid(route, true));
            Assert.DoesNotThrow(() => RouteMatcher.Parse(route, true));
            Assert.IsTrue(RouteMatcher.TryParse(route, true, out _));
        }

        [TestCase("/")] // Root.
        [TestCase("/abc/def")] // No parameters.
        [TestCase("/abc/{id}", "id")] // 1 parameter, takes a whole segment.
        [TestCase("/abc/{id?}", "id")] // 1 optional parameter, takes a whole segment.
        [TestCase("/a{id}", "id")] // 1 parameter, at start of segment.
        [TestCase("/{id}b", "id")] // 1 parameter, at end of segment.
        [TestCase("/a{id}b", "id")] // 1 parameter, mid-segment.
        [TestCase("/abc/{width}x{height}", "width", "height")] // 2 parameters, same segment.
        [TestCase("/abc/{width}/{height}", "width", "height")] // 2 parameters, different segments.
        [TestCase("/abc/{id}/{date?}", "id", "date")] // 2 parameters, different segments, 1 optional.
        public void RouteParameters_HaveCorrectNames(string route, params string[] parameterNames)
        {
            RouteMatcher.ClearCache();

            Assert.IsTrue(RouteMatcher.TryParse(route, false, out var matcher));
            Assert.AreEqual(parameterNames.Length, matcher.ParameterNames.Count);
            for (var i = 0; i < parameterNames.Length; i++)
                Assert.AreEqual(parameterNames[i], matcher.ParameterNames[i]);
        }

        [TestCase("/", "/")] // Root.
        [TestCase("/abc/def", "/abc/def")]
        [TestCase("/abc/{id}", "/abc/123", "id", "123")]
        [TestCase("/abc/{id?}", "/abc", "id", "")]
        [TestCase("/abc/{id}/{date}", "/abc/123/20190223", "id", "123", "date", "20190223")]
        [TestCase("/abc/{id}/{date?}", "/abc/123", "id", "123", "date", "")]
        [TestCase("/abc/{id?}/{date}", "/abc/20190223", "id", "", "date", "20190223")]
        public void MatchedRoute_HasCorrectParameters(string route, string path, params string[] parameters)
        {
            if (parameters.Length % 2 != 0)
                throw new InvalidOperationException("Parameters should be in name, value pairs.");

            RouteMatcher.ClearCache();

            var parameterCount = parameters.Length / 2;
            Assert.IsTrue(RouteMatcher.TryParse(route, false, out var matcher));
            Assert.AreEqual(parameterCount, matcher.ParameterNames.Count);
            for (var i = 0; i < parameterCount; i++)
                Assert.AreEqual(parameters[2 * i], matcher.ParameterNames[i]);

            var match = matcher.Match(path);
            Assert.IsNotNull(match);
            var keys = match.Keys.ToArray();
            var values = match.Values.ToArray();
            Assert.AreEqual(parameterCount, keys.Length);
            Assert.AreEqual(parameterCount, values.Length);
            for (var i = 0; i < parameterCount; i++)
            {
                Assert.AreEqual(parameters[2 * i], keys[i]);
                Assert.AreEqual(parameters[(2 * i) + 1], values[i]);
            }
        }

        [TestCase("/", "/", "/")]
        [TestCase("/", "/SUBPATH", "/SUBPATH")]
        [TestCase("/abc/def/", "/abc/def", "/")]
        [TestCase("/abc/def/", "/abc/def/SUBPATH", "/SUBPATH")]
        [TestCase("/abc/{id}/", "/abc/123", "/")]
        [TestCase("/abc/{id}/", "/abc/123/SUBPATH", "/SUBPATH")]
        [TestCase("/abc/{width}x{height}/", "/abc/123x456", "/")]
        [TestCase("/abc/{width}x{height}/", "/abc/123x456/SUBPATH", "/SUBPATH")]
        [TestCase("/abc/{id}/{date}/", "/abc/123/20190223", "/")]
        [TestCase("/abc/{id}/{date}/", "/abc/123/20190223/SUBPATH", "/SUBPATH")]
        public void MatchedBaseRoute_HasCorrectSubPath(string route, string path, string subPath)
        {
            RouteMatcher.ClearCache();

            Assert.IsTrue(RouteMatcher.TryParse(route, true, out var matcher));

            var match = matcher.Match(path);
            Assert.IsNotNull(match);
            Assert.AreEqual(subPath, match.SubPath);
        }
    }
}