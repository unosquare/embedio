## Overview
Sample app to show how to use [EmbedIO](https://github.com/unosquare/embedio) with Xamarin Forms

## How to
First of all, add two NuGet package to Forms project
- [EmbedIO](https://www.nuget.org/packages/EmbedIO/)
- [Xam.Plugin.WebView](https://www.nuget.org/packages/Xam.Plugin.WebView/)
 
After that, add a reference to Mono.Android.Export (Reference, not NuGet package) to Android project.

In this sample, there's a single index.html saying "Hello, World!" inside html folder. In the Android project, this folder must be places inside Assets folder. But in the iOS project, place the html folder inside Resources.

To show the WebView, there's the following XAML code

```xml
<?xml version="1.0" encoding="utf-8"?>
<ContentPage 
    xmlns="http://xamarin.com/schemas/2014/forms" 
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml" 
    xmlns:local="clr-namespace:EmbedIO.Forms.Sample" 
    x:Class="EmbedIO.Forms.Sample.MainPage"
    xmlns:wv="clr-namespace:Xam.Plugin.WebView.Abstractions;assembly=Xam.Plugin.WebView.Abstractions">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>
        <wv:FormsWebView x:Name="WebContent" 
                         ContentType="LocalFile" 
                         Grid.Row="0" Grid.Column="0" 
                         Source="html/index.html" />
    </Grid>
</ContentPage>
```

To start the WebServer, we have (at the App.xaml.cs file):

```csharp
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Modules;
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

            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
            // Handle when your app starts
            Task.Factory.StartNew(async () =>
            {
                using (var server = new WebServer("http://localhost:8080"))
                {
                    server.RegisterModule(new LocalSessionModule());
                    server.Module<StaticFilesModule>().UseRamCache = true;
                    server.Module<StaticFilesModule>().DefaultExtension = ".html";
                    server.Module<StaticFilesModule>().DefaultDocument = "index.html";
                    await server.RunAsync();
                }
            });
        }
    }
}
```