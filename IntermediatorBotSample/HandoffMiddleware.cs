using IntermediatorBotSample.CommandHandling;
using IntermediatorBotSample.MessageRouting;
using IntermediatorBotSample.Settings;
using Microsoft.Bot.Builder;
using System;
using System.Threading.Tasks;
using Underscore.Bot.MessageRouting;
using Underscore.Bot.MessageRouting.DataStore;
using Underscore.Bot.MessageRouting.DataStore.Azure;
using Underscore.Bot.MessageRouting.DataStore.Local;

namespace IntermediatorBotSample
{
    public class HandoffMiddleware : IMiddleware
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

        public CommandHandler CommandHandler
        {
            get;
            protected set;
        }

        public ConversationHistory.ConversationHistory ConversationHistory
        {
            get;
            protected set;
        }

        public HandoffMiddleware(BotSettings botSettings)
        {
            string connectionString = botSettings[BotSettings.KeyRoutingDataStoreConnectionString];
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
            CommandHandler = new CommandHandler(MessageRouter, MessageRouterResultHandler);
            ConversationHistory = new ConversationHistory.ConversationHistory(connectionString);
        }

        public Task OnTurn(ITurnContext context, MiddlewareSet.NextDelegate next)
        {
            throw new NotImplementedException();
        }
    }
}
