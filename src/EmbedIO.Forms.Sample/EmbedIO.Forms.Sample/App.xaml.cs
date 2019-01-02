using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Modules;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Reflection;

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
                using (var server = new WebServer("http://*:8080"))
                {
                    Assembly assembly = typeof(App).Assembly;
                    server.RegisterModule(new ResourceFilesModule(assembly, "EmbedIO.Forms.Sample.html"));

                    await server.RunAsync();
                }
            });

            MainPage = new MainPage();
        }
    }
}
