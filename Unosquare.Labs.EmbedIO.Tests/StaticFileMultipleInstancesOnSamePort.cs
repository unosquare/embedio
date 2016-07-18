using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Unosquare.Labs.EmbedIO.Modules;
using Unosquare.Labs.EmbedIO.Tests.Properties;
using Unosquare.Labs.EmbedIO.Tests.TestObjects;

namespace Unosquare.Labs.EmbedIO.Tests {

    public class StaticFileWebServerTestSubject {
        protected WebServer WebServer { get; set; }
        public string InstanceName { get; private set; }
        public string RootUrl { get; protected set; }
        protected TestConsoleLog Logger = new TestConsoleLog ();


        public StaticFileWebServerTestSubject (string instanceName) {
            this.InstanceName = instanceName;
            var rootPath = TestHelper.SetupStaticFolderInstance (InstanceName);
            RootUrl = Resources.ServerAddress + instanceName;
            WebServer = new WebServer (RootUrl, Logger);
            WebServer.RegisterModule (new StaticFilesModule (rootPath) { UseRamCache = true });
            WebServer.RunAsync ();
        }

        public void Kill () {
            WebServer?.Dispose ();
        }
    }

    [TestFixture]
    public class StaticFileMultipleInstancesOnSamePort {

        protected List<string> InstancesNames { get { return new List<string> () { String.Empty, "A/", "B/", "C/", "A/C", "AAA/A/B/C", "A/B/C/" }; } }
        protected List<StaticFileWebServerTestSubject> Subjects { get; set; } = new List<StaticFileWebServerTestSubject> ();

        [SetUp]
        public void Init () {
            Subjects.AddRange (InstancesNames.Select (x => new StaticFileWebServerTestSubject (x)));

        }

        [Test]
        public void FileContentsMatchInstanceName () {

            foreach (var item in Subjects) {
                var request = (HttpWebRequest)WebRequest.Create (item.RootUrl);
                using (var response = (HttpWebResponse)request.GetResponse ()) {
                    Assert.AreEqual (response.StatusCode, HttpStatusCode.OK, "Status Code OK");
                    var html = new StreamReader (response.GetResponseStream ()).ReadToEnd ();
                    Assert.AreEqual (html, TestHelper.GetStaticFolderInstanceIndexFileContents (item.InstanceName), "index.html contents match instance name");
                }

            }
        }


        [TearDown]
        public void Kill () {
            Thread.Sleep (TimeSpan.FromSeconds (1));
            Subjects.ForEach (x => x.Kill ());
        }
    }
}
