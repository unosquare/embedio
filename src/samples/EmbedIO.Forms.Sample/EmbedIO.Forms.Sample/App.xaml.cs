using EmbedIO.WebApi;
using System.Reflection;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace EmbedIO.Forms.Sample
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Server must be started, before WebView is initialized,
            // because we have no reload implemented in this sample.
            Task.Factory.StartNew(async () =>
            {
                using (var server = new WebServer(HttpListenerMode.EmbedIO, "http://*:8080"))
                {
                    Assembly assembly = typeof(App).Assembly;
                    server.WithLocalSessionManager();
                    server.WithWebApi("/api", m => m.WithController(() => new TestController()));
                    server.WithEmbeddedResources("/", assembly, "EmbedIO.Forms.Sample.html");
                    await server.RunAsync();
                }
            });

            MainPage = new MainPage();
        }
    }
}
