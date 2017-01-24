using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MessageRouting
{
    /// <summary>
    /// Provides the main interface for message routing.
    /// </summary>
    public class MessageRouterManager
    {
        /// <summary>
        /// Invoked when an engagement is initiated, established (added) or ended (removed).
        /// </summary>
        public event EventHandler<EngagementChangedEventArgs> EngagementChanged
        {
            add
            {
                RoutingDataManager.EngagementChanged += value;
            }
            remove
            {
                RoutingDataManager.EngagementChanged -= value;
            }
        }

        /// <summary>
        /// Invoked when an action cannot be executed due to this instance not being initialized.
        /// See IsInitialized property for more information.
        /// </summary>
        public event EventHandler<MessageRouterFailureEventArgs> NotInitialized;

        /// <summary>
        /// Invoked when this manager fails to forward a message.
        /// See HandleMessageAsync method for more information.
        /// </summary>
        public event EventHandler<MessageRouterFailureEventArgs> FailedToForwardMessage;

        private static MessageRouterManager _instance;
        /// <summary>
        /// The singleton instance of this class.
        /// </summary>
        public static MessageRouterManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MessageRouterManager();
                }

                return _instance;
            }
        }

        /// <summary>
        /// If true, the router needs an aggregation channel/conversation to be set before any
        /// routing can take place. True by default. It is recommended to set this value in
        /// App_Start/WebApiConfig.cs
        /// </summary>
        public bool AggregationRequired
        {
            get;
            set;
        }

        /// <summary>
        /// Handler for direct commands given to the bot.
        /// If you want to use your own command handler, it is recommended to set it to this
        /// property in App_Start/WebApiConfig.cs
        /// </summary>
        public IBotCommandHandler BotCommandHandler
        {
            get;
            set;
        }

        /// <summary>
        /// True, if this router manager instance is ready to serve customers. False otherwise.
        /// Note that if the aggregation is not required, this method will always return true.
        /// </summary>
        public bool IsInitialized
        {
            get
            {
                IList<Party> aggregationParties = RoutingDataManager.GetAggregationParties();
                return (!AggregationRequired || (aggregationParties != null && aggregationParties.Count() > 0));
            }
        }

        /// <summary>
        /// The routing data and all the parties the bot has seen including the instances of itself.
        /// </summary>
        public IRoutingDataManager RoutingDataManager
        {
            get;
            private set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        private MessageRouterManager()
        {
            AggregationRequired = true; // Do not change the value here!
            BotCommandHandler = new DefaultBotCommandHandler(); // You can override this by setting the value of the property afterwards

            // TODO: Get this instance from a data storage instead of keeping a local copy!
            RoutingDataManager = new LocalRoutingDataManager();
        }

        /// <summary>
        /// Checks the given parties and adds them to the collection, if not already there.
        /// 
        /// Note that this method expects that the recipient is the bot. The sender could also be
        /// the bot, but that case is checked before adding the sender to the container.
        /// </summary>
        /// <param name="senderParty">The sender party (from).</param>
        /// <param name="recipientParty">The recipient party.</param>
        public void MakeSurePartiesAreTracked(Party senderParty, Party recipientParty)
        {
            // Store the bot identity, if not already stored
            RoutingDataManager.AddParty(recipientParty, false);

            // Check that the party who sent the message is not the bot
            if (!RoutingDataManager.GetBotParties().Contains(senderParty))
            {
                // Store the user party, if not already stored
                RoutingDataManager.AddParty(senderParty);
            }
        }

        /// <summary>
        /// Checks the given activity for new parties and adds them to the collection, if not
        /// already there.
        /// </summary>
        /// <param name="activity">The activity.</param>
        public void MakeSurePartiesAreTracked(IActivity activity)
        {
            MakeSurePartiesAreTracked(
                MessagingUtils.CreateSenderParty(activity),
                MessagingUtils.CreateRecipientParty(activity));
        }

        /// <summary>
        /// Tries to send the given message activity to the given party using this bot on the same
        /// channel as the party who the message is sent to.
        /// </summary>
        /// <param name="partyToMessage">The party to send the message to.</param>
        /// <param name="messageActivity">The message activity to send (message content).</param>
        /// <returns>The ResourceResponse instance or null in case of an error.</returns>
        public async Task<ResourceResponse> SendMessageToPartyByBotAsync(Party partyToMessage, IMessageActivity messageActivity)
        {
            Party botParty = null;

            if (partyToMessage != null)
            {
                // We need the channel account of the bot in the SAME CHANNEL as the RECIPIENT.
                // The identity of the bot in the channel of the sender is most likely a different one and
                // thus unusable since it will not be recognized on the recipient's channel.
                botParty = RoutingDataManager.FindBotPartyByChannelAndConversation(
                    partyToMessage.ChannelId, partyToMessage.ConversationAccount);
            }

            if (botParty != null)
            {
                MessagingUtils.ConnectorClientAndMessageBundle bundle =
                    MessagingUtils.CreateConnectorClientAndMessageActivity(
                        partyToMessage.ServiceUrl, messageActivity);

                return await bundle.connectorClient.Conversations.SendToConversationAsync(
                    (Activity)bundle.messageActivity);
            }

            return null;
        }

        /// <summary>
        /// Tries to send the given message to the given party using this bot on the same channel
        /// as the party who the message is sent to.
        /// </summary>
        /// <param name="partyToMessage">The party to send the message to.</param>
        /// <param name="messageText">The message content.</param>
        /// <returns>The ResourceResponse instance or null in case of an error.</returns>
        public async Task<ResourceResponse> SendMessageToPartyByBotAsync(Party partyToMessage, string messageText)
        {
            Party botParty = null;

            if (partyToMessage != null)
            {
                botParty = RoutingDataManager.FindBotPartyByChannelAndConversation(
                    partyToMessage.ChannelId, partyToMessage.ConversationAccount);
            }

            if (botParty != null)
            {
                MessagingUtils.ConnectorClientAndMessageBundle bundle =
                    MessagingUtils.CreateConnectorClientAndMessageActivity(
                        partyToMessage, messageText, botParty?.ChannelAccount);

                return await bundle.connectorClient.Conversations.SendToConversationAsync(
                    (Activity)bundle.messageActivity);
            }

            return null;
        }

        /// <summary>
        /// Tries to initiates the engagement by creating a request on behalf of the sender in the
        /// given activity. This method does nothing, if a request for the same user already exists.
        /// </summary>
        /// <param name="activity">The activity.</param>
        /// <returns>True, if successful. False otherwise.</returns>
        public bool InitiateEngagement(Activity activity)
        {
            if (IsInitialized)
            {
                RoutingDataManager.AddPendingRequest(MessagingUtils.CreateSenderParty(activity));                
                return true;
            }

            NotInitialized?.Invoke(this, new MessageRouterFailureEventArgs()
            {
                Activity = activity,
                ErrorMessage = "Cannot initiate an engagement"
            });

            return false;
        }

        /// <summary>
        /// Tries to establish 1:1 chat between the two given parties.
        /// Note that the conversation owner will have a new separate party in the created engagement.
        /// </summary>
        /// <param name="conversationOwnerParty">The party who owns the conversation (e.g. customer service agent).</param>
        /// <param name="conversationClientParty">The other party in the conversation.</param>
        /// <returns>A valid ConversationResourceResponse of the newly created direct conversation
        /// (between the bot [who will relay messages] and the conversation owner),
        /// if the engagement was added and a conversation created successfully.
        /// False otherwise.</returns>
        private async Task<ConversationResourceResponse> AddEngagementAsync(
            Party conversationOwnerParty, Party conversationClientParty)
        {
            ConversationResourceResponse conversationResourceResponse = null;

            if (conversationOwnerParty == null || conversationClientParty == null)
            {
                throw new ArgumentNullException(
                    $"Neither of the arguments ({nameof(conversationOwnerParty)}, {nameof(conversationClientParty)}) can be null");
            }

            Party botParty = RoutingDataManager.FindBotPartyByChannelAndConversation(
                conversationOwnerParty.ChannelId, conversationOwnerParty.ConversationAccount);

            if (botParty != null)
            {
                ConnectorClient connectorClient = new ConnectorClient(new Uri(conversationOwnerParty.ServiceUrl));

                ConversationResourceResponse response =
                    await connectorClient.Conversations.CreateDirectConversationAsync(
                        botParty.ChannelAccount, conversationOwnerParty.ChannelAccount);

                if (response != null && !string.IsNullOrEmpty(response.Id))
                {
                    conversationResourceResponse = response;

                    // The conversation account of the conversation owner for this 1:1 chat is different -
                    // thus, we need to create a new party instance
                    Party acceptorPartyEngaged = new Party(
                        conversationOwnerParty.ServiceUrl, conversationOwnerParty.ChannelId,
                        conversationOwnerParty.ChannelAccount, new ConversationAccount(id: conversationResourceResponse.Id));

                    RoutingDataManager.AddParty(acceptorPartyEngaged);
                    RoutingDataManager.AddEngagementAndClearPendingRequest(acceptorPartyEngaged, conversationClientParty);
                }
            }

            return conversationResourceResponse;
        }

        /// <summary>
        /// Handles the incoming message activities. For instance, if it is a message from party
        /// engaged in a chat, the message will be forwarded to the counterpart in whatever
        /// channel that party is on.
        /// </summary>
        /// <param name="activity">The activity to handle.</param>
        /// <param name="addClientNameToMessage">If true, will add the client's name to the beginning of the message.</param>
        /// <param name="addOwnerNameToMessage">If true, will add the owner's (agent) name to the beginning of the message.</param>
        /// <returns>True, if the message was handled (forwared to the appropriate party).
        /// False otherwise (e.g. when the user is not engaged in a 1:1 conversation).</returns>
        public async Task<bool> HandleMessageAsync(
            Activity activity, bool addClientNameToMessage = true, bool addOwnerNameToMessage = false)
        {
            bool wasHandled = false;
            Party senderParty = MessagingUtils.CreateSenderParty(activity);

            if (RoutingDataManager.IsEngaged(senderParty, EngagementProfile.Owner))
            {
                // Sender is an owner of an ongoing conversation - forward the message
                Party partyToForwardMessageTo = RoutingDataManager.GetEngagedCounterpart(senderParty);

                if (partyToForwardMessageTo != null)
                {
                    string message = addOwnerNameToMessage
                        ? $"{senderParty.ChannelAccount.Name}: {activity.Text}" : activity.Text;
                    ResourceResponse resourceResponse =
                        await SendMessageToPartyByBotAsync(partyToForwardMessageTo, activity.Text);

                    if (!MessagingUtils.WasSuccessful(resourceResponse))
                    {
                        FailedToForwardMessage?.Invoke(this, new MessageRouterFailureEventArgs()
                        {
                            Activity = activity,
                            ErrorMessage = $"Failed to forward the message to user {partyToForwardMessageTo}"
                        });
                    }
                }

                wasHandled = true;
            }
            else if (RoutingDataManager.IsEngaged(senderParty, EngagementProfile.Client))
            {
                // Sender is a participant of an ongoing conversation - forward the message
                Party partyToForwardMessageTo = RoutingDataManager.GetEngagedCounterpart(senderParty);

                if (partyToForwardMessageTo != null)
                {
                    string message = addClientNameToMessage
                        ? $"{senderParty.ChannelAccount.Name}: {activity.Text}" : activity.Text;
                    await SendMessageToPartyByBotAsync(partyToForwardMessageTo, message);
                }

                wasHandled = true;
            }
            else if (!IsInitialized)
            {
                // No aggregation channel set up
                NotInitialized?.Invoke(this, new MessageRouterFailureEventArgs()
                {
                    Activity = activity,
                    ErrorMessage = "Failed to handle the message"
                });
            }

            return wasHandled;
        }

        /// <summary>
        /// Handles the request acceptance and tries to establish 1:1 chat between the two given
        /// parties. This method is only used when the aggregation is enabled.
        /// </summary>
        /// <param name="acceptorParty">The party accepting the request.</param>
        /// <param name="partyToAccept">The party to be accepted.</param>
        /// <returns>True, if the accepted request was handled successfully. False otherwise.</returns>
        public async Task<bool> HandleAcceptedRequestAsync(Party acceptorParty, Party partyToAccept)
        {
            ConversationResourceResponse conversationResourceResponse =
                await AddEngagementAsync(acceptorParty, partyToAccept);

            return (conversationResourceResponse != null);
        }
    }
}