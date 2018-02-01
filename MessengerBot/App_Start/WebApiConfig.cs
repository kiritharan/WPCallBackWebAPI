using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace MessengerBot
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // config.Routes.IgnoreRoute("", "");

            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}", // go to Web API controller           
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
