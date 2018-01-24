using IntermediatorBotSample.CommandHandling;
using IntermediatorBotSample.MessageRouting;
using IntermediatorBotSample.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Web.Http;
using Underscore.Bot.MessageRouting;
using Underscore.Bot.MessageRouting.DataStore;
using Underscore.Bot.MessageRouting.DataStore.Azure;
using Underscore.Bot.MessageRouting.DataStore.Local;

namespace IntermediatorBotSample
{
    public static class WebApiConfig
    {
        public static MessageRouterManager MessageRouterManager
        {
            get;
            private set;
        }

        public static MessageRouterResultHandler MessageRouterResultHandler
        {
            get;
            private set;
        }

        public static CommandMessageHandler CommandMessageHandler
        {
            get;
            private set;
        }

        public static BackChannelMessageHandler BackChannelMessageHandler
        {
            get;
            private set;
        }

        public static BotSettings Settings
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
                Formatting = Formatting.Indented,
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
            InitializeMessageRouting();
        }

        /// <summary>
        /// Creates and sets up the instances required for message routing.
        /// </summary>
        public static void InitializeMessageRouting()
        {
            Settings = new BotSettings();
            string connectionString = Settings[BotSettings.KeyRoutingDataStorageConnectionString];
            IRoutingDataManager routingDataManager = null;

            if (string.IsNullOrEmpty(connectionString))
            {
                System.Diagnostics.Debug.WriteLine($"WARNING!!! No connection string found - using {nameof(LocalRoutingDataManager)}");
                routingDataManager = new LocalRoutingDataManager();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Found a connection string - using {nameof(AzureTableStorageRoutingDataManager)}");
                routingDataManager = new AzureTableStorageRoutingDataManager(connectionString);
            }

            MessageRouterManager = new MessageRouterManager(routingDataManager);
            MessageRouterResultHandler = new MessageRouterResultHandler(MessageRouterManager);
            CommandMessageHandler = new CommandMessageHandler(MessageRouterManager, MessageRouterResultHandler);
            BackChannelMessageHandler = new BackChannelMessageHandler(MessageRouterManager.RoutingDataManager);
        }
    }
}
