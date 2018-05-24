using IntermediatorBotSample.CommandHandling;
using IntermediatorBotSample.MessageRouting;
using IntermediatorBotSample.Settings;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Threading.Tasks;
using Underscore.Bot.MessageRouting;
using Underscore.Bot.MessageRouting.DataStore;
using Underscore.Bot.MessageRouting.DataStore.Azure;
using Underscore.Bot.MessageRouting.DataStore.Local;
using Underscore.Bot.MessageRouting.Results;

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

        private BotSettings _botSettings;

        public HandoffMiddleware(BotSettings botSettings)
        {
            _botSettings = botSettings;
            string connectionString = _botSettings[BotSettings.KeyRoutingDataStoreConnectionString];
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

        public async Task OnTurn(ITurnContext context, MiddlewareSet.NextDelegate next)
        {
            Activity activity = context.Activity;

            if (activity.Type is ActivityTypes.Message)
            {
                bool rejectConnectionRequestIfNoAggregationChannel =
                    _botSettings.RejectConnectionRequestIfNoAggregationChannel;

                MessageRouter.StoreConversationReferences(activity);

                AbstractMessageRouterResult messageRouterResult = null;

                if (await CommandHandler.HandleCommandAsync(activity) == false)
                {
                    // No command detected/handled

                    // Let the message router handle the activity
                    messageRouterResult = await MessageRouter.HandleActivityAsync(
                        activity, false, rejectConnectionRequestIfNoAggregationChannel);

                    if (messageRouterResult is MessageRoutingResult
                        && (messageRouterResult as MessageRoutingResult).Type == MessageRoutingResultType.NoActionTaken)
                    {
                        // No action was taken by the message router. This means that the user
                        // is not connected (in a 1:1 conversation) with a human
                        // (e.g. customer service agent) yet.

                        // Check for cry for help (agent assistance)
                        if (!string.IsNullOrWhiteSpace(activity.Text)
                            && activity.Text.ToLower().Contains("human"))
                        {
                            // Create a connection request on behalf of the sender
                            // Note that the returned result must be handled
                            messageRouterResult = MessageRouter.CreateConnectionRequest(
                                MessageRouter.CreateSenderConversationReference(activity),
                                rejectConnectionRequestIfNoAggregationChannel);
                        }
                        else
                        {
                            // No action taken - this middleware did not consume the activity so let it propagate
                            await next().ConfigureAwait(false);
                        }
                    }
                }

                // Uncomment to see the result in a reply (may be useful for debugging)
                //if (messageRouterResult != null)
                //{
                //    await MessageRouter.ReplyToActivityAsync(activity, messageRouterResult.ToString());
                //}

                // Handle the result, if necessary
                await MessageRouterResultHandler.HandleResultAsync(messageRouterResult);
            }
        }
    }
}
