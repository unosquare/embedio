using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using Unosquare.Labs.EmbedIO.Modules;
using Unosquare.Labs.EmbedIO.Tests.TestObjects;
using Unosquare.Swan;

namespace Unosquare.Labs.EmbedIO.Tests
{
    public static class Program
    {
        public static void Main()
        {
            var url = Resources.GetServerAddress();

            using (var instance = new WebServer(url))
            {
                instance.RegisterModule(new TestWebModule());
                instance.RunAsync();

                var request = (HttpWebRequest)WebRequest.Create(url + TestWebModule.RedirectAbsoluteUrl);
#if NET452
                request.AllowAutoRedirect = false;
#endif
                using (var response = (HttpWebResponse)request.GetResponseAsync().Result)
                {
                    Assert.AreEqual(response.StatusCode, HttpStatusCode.Redirect, "Status Code Redirect");
                }
            }

            Console.ReadKey();
        }
    }
}