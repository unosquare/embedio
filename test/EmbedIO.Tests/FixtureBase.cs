﻿using System;
using System.Net.Http;
using System.Threading.Tasks;
using EmbedIO.Tests.Internal;
using EmbedIO.Tests.TestObjects;
using EmbedIO.Utilities;
using NUnit.Framework;
using Unosquare.Swan;

namespace EmbedIO.Tests
{
    public abstract class FixtureBase : IDisposable
    {
        private readonly Action<IWebServer> _builder;
        private readonly bool _useTestWebServer;

        protected FixtureBase(Action<IWebServer> builder, bool useTestWebServer = false)
        {
            Terminal.Settings.GlobalLoggingMessageType = LogMessageType.None;

            _builder = builder;
            _useTestWebServer = useTestWebServer;
        }

        ~FixtureBase()
        {
            Dispose(false);
        }

        public string WebServerUrl { get; private set; }

        public IWebServer WebServerInstance { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [SetUp]
        public void Init()
        {
            WebServerUrl = Resources.GetServerAddress();
            WebServerInstance = _useTestWebServer
                ? new TestWebServer() as IWebServer
                : new WebServer(WebServerUrl);

            _builder(WebServerInstance);
            OnAfterInit();
            WebServerInstance.RunAsync();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            WebServerInstance?.Dispose();
        }

        protected virtual void OnAfterInit()
        {
        }

        [TearDown]
        public void Kill()
        {
            Task.Delay(500).Await();
            WebServerInstance?.Dispose();
        }

        public async Task<string> GetString(string partialUrl = "")
        {
            if (WebServerInstance is TestWebServer testWebServer)
                return await testWebServer.GetClient().GetAsync(partialUrl);

            using (var client = new HttpClient())
            {
                var uri = new Uri(new Uri(WebServerUrl), partialUrl);

                return await client.GetStringAsync(uri);
            }
        }

        public async Task<TestHttpResponse> SendAsync(TestHttpRequest request)
        {
            if (WebServerInstance is TestWebServer testWebServer)
                return await testWebServer.GetClient().SendAsync(request);

            using (var client = new HttpClient())
            {
                var response = await client.SendAsync(request.ToHttpRequestMessage());

                return response.ToTestHttpResponse();
            }
        }
    }

    internal static class TestExtensions
    {
        public static HttpRequestMessage ToHttpRequestMessage(this TestHttpRequest request)
        {
            return new HttpRequestMessage();
        }

        public static TestHttpResponse ToTestHttpResponse(this HttpResponseMessage response)
        {
            return new TestHttpResponse();
        }
    }
}