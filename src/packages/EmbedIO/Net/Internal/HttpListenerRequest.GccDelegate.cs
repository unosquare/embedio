using System.Security.Cryptography.X509Certificates;

namespace EmbedIO.Net.Internal
{
    partial class HttpListenerRequest
    {
        private delegate X509Certificate2 GccDelegate();
    }
}