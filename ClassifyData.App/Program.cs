using System;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using ClassifyData.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Vidyano.Service;

namespace ClassifyData.App
{
    static class Program
    {
        public static readonly string BaseAddress = "http://localhost:9987/";

        [STAThread]
        static void Main(string[] args)
        {
            // TODO: Show information when application fails to startup

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = args,
                ContentRootPath = AppContext.BaseDirectory,
            });
            builder.WebHost.UseUrls(BaseAddress); // TODO: Try other ports in case 9000 is in use
            builder.Services.AddVidyanoNoDatabase(builder.Configuration);
            builder.Services.AddScoped<ClassifyDataEntityModelContainer>();
            builder.Services.AddTransient<RequestScopeProvider<ClassifyDataEntityModelContainer>>();

            var app = builder.Build();
            app.UseVidyano(app.Environment, app.Configuration);
            app.StartAsync().GetAwaiter().GetResult();

            Cef.Initialize(new CefSettings());

            Application.Run(new AppForm());

            app.StopAsync().GetAwaiter().GetResult();
        }
    }
}
