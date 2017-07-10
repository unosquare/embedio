using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Unosquare.Labs.EmbedIO.Modules;
using Unosquare.Labs.EmbedIO.Tests.TestObjects;
using Unosquare.Swan.Formatters;

namespace Unosquare.Labs.EmbedIO.Tests
{
    public static class Program
    {
        public static void Main()
        {
            Task.Factory.StartNew(async () =>
                {
                    var encodeName = "iso-8859-1";

                    var url = Resources.GetServerAddress();

                    using (var instance = new WebServer(url))
                    {
                        instance.RegisterModule(new FallbackModule((ctx, ct) =>
                        {
                            var encoding = Encoding.GetEncoding("UTF-8");

                            try
                            {
                                var encodeValue =
                                    ctx.Request.ContentType.Split(';')
                                        .FirstOrDefault(
                                            x => x.Trim().StartsWith("charset", StringComparison.OrdinalIgnoreCase))
                                        ?.Split('=')
                                        .Skip(1)
                                        .FirstOrDefault()?
                                        .Trim();
                                encoding = Encoding.GetEncoding(encodeValue);
                            }
                            catch
                            {
                                Assert.Inconclusive("Invalid encoding in system");
                            }

                            ctx.JsonResponse(new WebServerTest.EncodeCheck
                            {
                                Encoding = encoding.EncodingName,
                                IsValid = ctx.Request.ContentEncoding.EncodingName == encoding.EncodingName
                            });

                            return true;
                        }));

                        var runTask = instance.RunAsync();

                        var request = (HttpWebRequest)WebRequest.Create(url + TestWebModule.RedirectUrl);
                        request.ContentType = $"application/json; charset={encodeName}";

                        using (var response = (HttpWebResponse)await request.GetResponseAsync())
                        {
                            using (var ms = new MemoryStream())
                            {
                                response.GetResponseStream()?.CopyTo(ms);
                                var data = Encoding.UTF8.GetString(ms.ToArray());

                                Assert.IsNotNull(data, "Data is not empty");
                                var model = Json.Deserialize<WebServerTest.EncodeCheck>(data);

                                Assert.IsNotNull(model);
                                Assert.IsTrue(model.IsValid);
                            }
                        }
                    }
                });
            Console.ReadKey();
        }
    }
}