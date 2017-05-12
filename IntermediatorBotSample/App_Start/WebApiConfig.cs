using IntermediatorBotSample.CommandHandling;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Web.Http;
using Underscore.Bot.MessageRouting;

namespace IntermediatorBotSample
{
    public static class WebApiConfig
    {
        private static MessageRouterManager _messageRouterManager;
        public static MessageRouterManager MessageRouterManager
        {
            get
            {
                return _messageRouterManager;
            }
            private set
            {
                _messageRouterManager = value;
            }
        }

        private static BotCommandHandler _botCommandHandler;
        public static BotCommandHandler BotCommandHandler
        {
            get
            {
                return _botCommandHandler;
            }
            private set
            {
                _botCommandHandler = value;
            }
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
            BotCommandHandler = new BotCommandHandler(MessageRouterManager.RoutingDataManager);
        }
    }
}
