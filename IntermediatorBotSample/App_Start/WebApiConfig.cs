using IntermediatorBotSample.CommandHandling;
using IntermediatorBotSample.MessageRouting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Web.Http;
using Underscore.Bot.MessageRouting;
using Underscore.Bot.MessageRouting.DataStore;

namespace IntermediatorBotSample
{
    public static class WebApiConfig
    {
        public static MessageRouterManager MessageRouterManager
        {
            get;
            private set;
        }

        public static IMessageRouterResultHandler MessageRouterResultHandler
        {
            get;
            private set;
        }

        public static BotCommandHandler BotCommandHandler
        {
            get;
            private set;
        }

        public static void Register(HttpConfiguration config)
        {
            // Json settings
            config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Formatters.JsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Newtonsoft.Json.Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
            };

            // Web API configuration and services
            config.EnableCors();

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // Message routing
            MessageRouterManager = new MessageRouterManager(new LocalRoutingDataManager());
            MessageRouterResultHandler = new MessageRouterResultHandler();
            BotCommandHandler = new BotCommandHandler(MessageRouterManager.RoutingDataManager);
        }
    }
}
