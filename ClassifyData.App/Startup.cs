using System.Web.Http;
using Owin;
using Vidyano.Service;

namespace ClassifyData.App
{
    public class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            var config = new HttpConfiguration();
            config.Routes.MapVidyanoRoute();

            appBuilder.UseWebApi(config);
        }
    }
}