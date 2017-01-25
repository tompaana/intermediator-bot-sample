using Microsoft.Bot.Connector;
using System;
using System.Threading.Tasks;

namespace MessageRouting
{
    public class DefaultMessageRouterEventHandler : IDisposable
    {
        /// <summary>
        /// True, if the events from the message router manager have been hooked.
        /// </summary>
        public bool IsInitialized
        {
            get;
            protected set;
        }

        private static DefaultMessageRouterEventHandler _instance = null;
        /// <summary>
        /// Singleton instance of this class.
        /// </summary>
        public static DefaultMessageRouterEventHandler Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DefaultMessageRouterEventHandler(true);
                }

                return _instance;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="initializeInConstruction">If true, will initialize this instance right away.</param>
        private DefaultMessageRouterEventHandler(bool initializeInConstruction)
        {
            if (initializeInConstruction)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Hooks the events from the message router manager.
        /// </summary>
        public virtual void Initialize()
        {
            if (!IsInitialized)
            {
                MessageRouterManager messageRouterManager = MessageRouterManager.Instance;
                messageRouterManager.EngagementChanged += OnEngagementChangedAsync;
                messageRouterManager.NotInitialized += OnNotInitializedAsync;
                messageRouterManager.FailedToForwardMessage += OnFailedToForwardMessageAsync;
                IsInitialized = true;
                System.Diagnostics.Debug.WriteLine("Message router event handler initialized");
            }
        }

        /// <summary>
        /// From IDisposable.
        /// 
        /// Unhooks the events from the message router manager.
        /// </summary>
        public virtual void Dispose()
        {
            if (IsInitialized)
            {
                MessageRouterManager messageRouterManager = MessageRouterManager.Instance;
                messageRouterManager.EngagementChanged -= OnEngagementChangedAsync;
                messageRouterManager.NotInitialized -= OnNotInitializedAsync;
                messageRouterManager.FailedToForwardMessage -= OnFailedToForwardMessageAsync;
                IsInitialized = false;
                System.Diagnostics.Debug.WriteLine("Message router event handler disposed");
            }
        }

        /// <summary>
        /// Notifies both the conversation owner (agent) and the conversation client (customer)
        /// about the change in engagement (initiated/started/ended).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual async void OnEngagementChangedAsync(object sender, EngagementChangedEventArgs e)
        {
            MessageRouterManager messageRouterManager = MessageRouterManager.Instance;
            IRoutingDataManager routingDataManager = messageRouterManager.RoutingDataManager;

            Party conversationOwnerParty = e.ConversationOwnerParty;
            Party conversationClientParty = e.ConversationClientParty;

            string conversationOwnerName = conversationOwnerParty.ChannelAccount.Name;
            string conversationClientName = conversationClientParty.ChannelAccount.Name;

            string messageToConversationOwner = string.Empty;
            string messageToConversationClient = string.Empty;

            if (e.ChangeType == ChangeTypes.Initiated)
            {
                if (messageRouterManager.AggregationRequired)
                {
                    foreach (Party aggregationParty in messageRouterManager.RoutingDataManager.GetAggregationParties())
                    {
                        string botName = routingDataManager.ResolveBotNameInConversation(aggregationParty);
                        string commandKeyword = string.IsNullOrEmpty(botName)
                            ? messageRouterManager.BotCommandHandler.GetCommandKeyword() : $"@{botName}";

                        // TODO: The user experience here can be greatly improved by instead of sending
                        // a message displaying a card with buttons "accept" and "reject" on it
                        await messageRouterManager.SendMessageToPartyByBotAsync(aggregationParty,
                            $"User \"{conversationClientName}\" requests a chat; type \"{commandKeyword} accept {conversationClientName}\" to accept");
                    }
                }

                messageToConversationClient = "Please wait for your request to be accepted";
            }
            else if (e.ChangeType == ChangeTypes.Added)
            {
                if (messageRouterManager.AggregationRequired)
                {
                    messageToConversationOwner = $"Request from user \"{conversationClientName}\" accepted, feel free to start the chat";
                    messageToConversationClient = $"Your request has been accepted by {conversationOwnerName}, feel free to start the chat";
                }
                else
                {
                    messageToConversationOwner = $"You are now connected to user \"{conversationClientName}\"";
                    messageToConversationClient = $"You are now chatting with {conversationOwnerName}";
                }
            }
            else if (e.ChangeType == ChangeTypes.Removed)
            {
                messageToConversationOwner = $"You are now disconnected from the conversation with user \"{conversationClientName}\"";
                messageToConversationClient = $"Your conversation with {conversationOwnerName} has ended";
            }

            if (!string.IsNullOrEmpty(messageToConversationOwner))
            {
                await messageRouterManager.SendMessageToPartyByBotAsync(conversationOwnerParty, messageToConversationOwner);
            }

            if (!string.IsNullOrEmpty(messageToConversationClient))
            {
                await messageRouterManager.SendMessageToPartyByBotAsync(conversationClientParty, messageToConversationClient);
            }
        }

        /// <summary>
        /// Notifies the user that the message router manager is not initialized.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual async void OnNotInitializedAsync(object sender, MessageRouterFailureEventArgs e)
        {
            if (e != null && e.Activity != null)
            {
                MessageRouterManager messageRouterManager = MessageRouterManager.Instance;
                string botName = messageRouterManager.RoutingDataManager.ResolveBotNameInConversation(MessagingUtils.CreateSenderParty(e.Activity));

                string message = $"{(string.IsNullOrEmpty(e.ErrorMessage) ? "" : $"{e.ErrorMessage}: ")}The message router manager is not initialized; type \"";
                message += string.IsNullOrEmpty(botName) ? $"{messageRouterManager.BotCommandHandler.GetCommandKeyword()} " : $"@{botName} ";
                message += "init\" to setup the aggregation channel";

                await ReplyToActivityAsync(e.Activity, message);
            }
        }

        protected virtual async void OnFailedToForwardMessageAsync(object sender, MessageRouterFailureEventArgs e)
        {
            if (e != null)
            {
                MessageRouterManager messageRouterManager = MessageRouterManager.Instance;
                string message = $"{(string.IsNullOrEmpty(e.ErrorMessage) ? "Failed to forward the message" : e.ErrorMessage)}";
                await ReplyToActivityAsync(e.Activity, message);
            }
        }

        /// <summary>
        /// Replies to the given activity with the given message.
        /// </summary>
        /// <param name="activity">The activity to reply to.</param>
        /// <param name="message">The message.</param>
        protected async Task ReplyToActivityAsync(Activity activity, string message)
        {
            if (activity != null && !string.IsNullOrEmpty(message))
            {
                Activity replyActivity = activity.CreateReply(message);
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                await connector.Conversations.ReplyToActivityAsync(replyActivity);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Either the activity is null or the message is empty - Activity: {activity}; message: {message}");
            }
        }
    }
}
