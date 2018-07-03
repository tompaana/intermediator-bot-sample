using IntermediatorBotSample.CommandHandling;
using IntermediatorBotSample.ConversationHistory;
using IntermediatorBotSample.MessageRouting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using Underscore.Bot.MessageRouting;
using Underscore.Bot.MessageRouting.DataStore;
using Underscore.Bot.MessageRouting.DataStore.Azure;
using Underscore.Bot.MessageRouting.DataStore.Local;
using Underscore.Bot.MessageRouting.Results;

namespace IntermediatorBotSample.Middleware
{
    public class HandoffMiddleware : IMiddleware
    {
        private const string KeyAzureTableStorageConnectionString = "AzureTableStorageConnectionString";
        private const string KeyRejectConnectionRequestIfNoAggregationChannel = "RejectConnectionRequestIfNoAggregationChannel";
        private const string KeyPermittedAggregationChannels = "PermittedAggregationChannels";
        private const string KeyNoDirectConversationsWithChannels = "NoDirectConversationsWithChannels";

        public IConfiguration Configuration
        {
            get;
            protected set;
        }

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

        public MessageLogs MessageLogs
        {
            get;
            protected set;
        }

        public HandoffMiddleware(IConfiguration configuration)
        {
            Configuration = configuration;
            string connectionString = Configuration[KeyAzureTableStorageConnectionString];
            IRoutingDataStore routingDataStore = null;

            if (string.IsNullOrEmpty(connectionString))
            {
                System.Diagnostics.Debug.WriteLine($"WARNING!!! No connection string found - using {nameof(InMemoryRoutingDataStore)}");
                routingDataStore = new InMemoryRoutingDataStore();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Found a connection string - using {nameof(AzureTableRoutingDataStore)}");
                routingDataStore = new AzureTableRoutingDataStore(connectionString);
            }

            MessageRouter = new MessageRouter(
                routingDataStore,
                new MicrosoftAppCredentials(Configuration["MicrosoftAppId"], Configuration["MicrosoftAppPassword"]));

            //MessageRouter.Logger = new Logging.AggregationChannelLogger(MessageRouter);

            MessageRouterResultHandler = new MessageRouterResultHandler(MessageRouter);

            ConnectionRequestHandler connectionRequestHandler =
                new ConnectionRequestHandler(GetChannelList(KeyNoDirectConversationsWithChannels));

            CommandHandler = new CommandHandler(
                MessageRouter,
                MessageRouterResultHandler,
                connectionRequestHandler,
                GetChannelList(KeyPermittedAggregationChannels));

            MessageLogs = new MessageLogs(connectionString);
        }

        public async Task OnTurn(ITurnContext context, MiddlewareSet.NextDelegate next)
        {
            Activity activity = context.Activity;

            if (activity.Type is ActivityTypes.Message)
            {
                bool.TryParse(
                    Configuration[KeyRejectConnectionRequestIfNoAggregationChannel],
                    out bool rejectConnectionRequestIfNoAggregationChannel);

                // Store the conversation references (identities of the sender and the recipient [bot])
                // in the activity
                MessageRouter.StoreConversationReferences(activity);

                AbstractMessageRouterResult messageRouterResult = null;

                // Check the activity for commands
                if (await CommandHandler.HandleCommandAsync(context) == false)
                {
                    // No command detected/handled

                    // Let the message router route the activity, if the sender is connected with
                    // another user/bot
                    messageRouterResult = await MessageRouter.RouteMessageIfSenderIsConnectedAsync(activity);

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

        /// <summary>
        /// Extracts the channel list from the settings matching the given key.
        /// </summary>
        /// <returns>The list of channels or null, if none found.</returns>
        private IList<string> GetChannelList(string key)
        {
            IList<string> channelList = null;

            string channels = Configuration[key];

            if (!string.IsNullOrWhiteSpace(channels))
            {
                System.Diagnostics.Debug.WriteLine($"Channels by key \"{key}\": {channels}");
                string[] channelArray = channels.Split(',');

                if (channelArray.Length > 0)
                {
                    channelList = new List<string>();

                    foreach (string channel in channelArray)
                    {
                        channelList.Add(channel.Trim());
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"No channels defined by key \"{key}\" in app settings");
            }

            return channelList;
        }
    }
}
