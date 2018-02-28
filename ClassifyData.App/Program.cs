using System;
using System.Windows.Forms;
using CefSharp;
using Microsoft.Owin.Hosting;

namespace ClassifyData.App
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            var baseAddress = "http://localhost:9000/";

            using (WebApp.Start<Startup>(baseAddress))
            {
                var settings = new CefSettings();
                settings.BrowserSubprocessPath = @"x86\CefSharp.BrowserSubprocess.exe";

                Cef.Initialize(settings, performDependencyCheck: false, browserProcessHandler: null);

                var browser = new AppForm();
                Application.Run(browser);
            }
        }
    }
}