using IntermediatorBotSample.CommandHandling;
using IntermediatorBotSample.MessageRouting;
using IntermediatorBotSample.Settings;
using System;
using Underscore.Bot.MessageRouting;
using Underscore.Bot.MessageRouting.DataStore;
using Underscore.Bot.MessageRouting.DataStore.Azure;
using Underscore.Bot.MessageRouting.DataStore.Local;

namespace IntermediatorBotSample
{
    public class HandoffHelper
    {
        public MessageRouter MessageRouter
        {
            get;
            protected set;
        }

        public MessageRouterResultHandler MessageRouterResultHandler
        {
            get;
            protected set;
        }

        public CommandMessageHandler CommandMessageHandler
        {
            get;
            protected set;
        }

        public BackChannelMessageHandler BackChannelMessageHandler
        {
            get;
            protected set;
        }

        public HandoffHelper(BotSettings botSettings)
        {
            string connectionString = botSettings[BotSettings.KeyRoutingDataStorageConnectionString];
            IRoutingDataStore routingDataStore = null;

            if (string.IsNullOrEmpty(connectionString))
            {
                System.Diagnostics.Debug.WriteLine($"WARNING!!! No connection string found - using {nameof(InMemoryRoutingDataStore)}");
                routingDataStore = new InMemoryRoutingDataStore();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Found a connection string - using {nameof(AzureTableStorageRoutingDataStore)}");
                routingDataStore = new AzureTableStorageRoutingDataStore(connectionString);
            }

            MessageRouter = new MessageRouter(routingDataStore);
            MessageRouterResultHandler = new MessageRouterResultHandler(MessageRouter);
            CommandMessageHandler = new CommandMessageHandler(MessageRouter, MessageRouterResultHandler);
            BackChannelMessageHandler = new BackChannelMessageHandler(MessageRouter.RoutingDataManager);
        }
    }
}
