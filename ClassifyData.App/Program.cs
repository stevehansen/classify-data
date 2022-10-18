using System;
using System.IO;
using System.Web.Http;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using Microsoft.Owin.Hosting;
using Owin;
using Vidyano.Service;

namespace ClassifyData.App
{
    static class Program
    {
        public static readonly string BaseAddress = "http://localhost:9987/";

        [STAThread]
        static void Main()
        {
            // TODO: Show information when application fails to startup

            using (WebApp.Start(BaseAddress, Configuration)) // TODO: Try other ports in case 9000 is in use
            {
                var settings = new CefSettings();
                settings.BrowserSubprocessPath = Path.GetFullPath(@"x86\CefSharp.BrowserSubprocess.exe");

                Cef.Initialize(settings);

                var browser = new AppForm();
                Application.Run(browser);
            }
        }

        private static void Configuration(IAppBuilder appBuilder)
        {
            var config = new HttpConfiguration();
            config.Routes.MapVidyanoRoute();

            appBuilder.UseWebApi(config);
        }
    }
}