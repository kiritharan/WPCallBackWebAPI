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
              //     routeTemplate: "api/get/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            //config.Routes.MapHttpRoute(
            // name: "Manage",
            // routeTemplate: "Manage/{action}/{id}", // go to MVC controller
            //  defaults: new { controller = "Manage", action = "Configure", id = RouteParameter.Optional }
            //);

        }
    }
}
