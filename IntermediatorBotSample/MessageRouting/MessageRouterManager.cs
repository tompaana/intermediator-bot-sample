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
        /// The routing data and all the parties the bot has seen including the instances of itself.
        /// </summary>
        public IRoutingDataManager RoutingDataManager
        {
            get;
            private set;
        }

        private const string CommandKeyword = "command ";

        /// <summary>
        /// Constructor.
        /// </summary>
        private MessageRouterManager()
        {
            AggregationRequired = true; // Do not change the value here!

            // TODO: Get this instance from a data storage instead of keeping a local copy!
            RoutingDataManager = new LocalRoutingDataManager();
        }

        public bool IsInitialized()
        {
            IList<Party> aggregationParties = RoutingDataManager.GetAggregationParties();
            return (!AggregationRequired || (aggregationParties != null && aggregationParties.Count() > 0));
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
        /// Tries to resolve the name of the bot in the same conversation with the given party.
        /// </summary>
        /// <param name="party"></param>
        /// <returns>The name of the bot or null, if unable to resolve.</returns>
        public string ResolveBotNameInConversation(Party party)
        {
            string botName = null;

            if (party != null)
            {
                Party botParty = RoutingDataManager.FindBotPartyByChannelAndConversation(party.ChannelId, party.ConversationAccount);

                if (botParty != null && botParty.ChannelAccount != null)
                {
                    botName = botParty.ChannelAccount.Name;
                }
            }

            return botName;
        }

        /// <summary>
        /// Checks the given activity and determines whether the message was addressed directly to
        /// the bot or not.
        /// 
        /// Note: Only mentions are inspected at the moment.
        /// </summary>
        /// <param name="messageActivity">The message activity.</param>
        /// <returns>True, if the message was address directly to the bot. False otherwise.</returns>
        public bool WasBotAddressedDirectly(IMessageActivity messageActivity)
        {
            bool botWasMentioned = false;
            Mention[] mentions = messageActivity.GetMentions();

            foreach (Mention mention in mentions)
            {
                foreach (Party botParty in RoutingDataManager.GetBotParties())
                {
                    if (mention.Mentioned.Id.Equals(botParty.ChannelAccount.Id))
                    {
                        botWasMentioned = true;
                        break;
                    }
                }
            }

            return botWasMentioned;
        }

        /// <summary>
        /// Tries to send the given message to the given party using this bot on the same channel
        /// as the party who the message is sent to.
        /// </summary>
        /// <param name="partyToMessage">The party to send the message to.</param>
        /// <param name="messageText">The message content.</param>
        /// <returns>The APIResponse instance.</returns>
        public async Task<ResourceResponse> SendMessageToPartyByBotAsync(Party partyToMessage, string messageText)
        {
            // We need the channel account of the bot in the SAME CHANNEL as the RECIPIENT.
            // The identity of the bot in the channel of the sender is most likely a different one and
            // thus unusable since it will not be recognized on the recipient's channel.
            Party botParty = RoutingDataManager.FindBotPartyByChannelAndConversation(
                partyToMessage.ChannelId, partyToMessage.ConversationAccount);

            MessagingUtils.ConnectorClientAndMessageBundle bundle =
                MessagingUtils.CreateConnectorClientAndMessageActivity(
                    partyToMessage, messageText, botParty?.ChannelAccount);

            return await bundle.connectorClient.Conversations.SendToConversationAsync(
                (Activity)bundle.messageActivity);
        }

        /// <summary>
        /// Tries to initiates the engagement by creating a request on behalf of the sender in the
        /// given activity.
        /// </summary>
        /// <param name="activity"></param>
        /// <returns>True, if successful. False otherwise.</returns>
        public async Task<bool> InitiateEngagementAsync(Activity activity)
        {
            Party senderParty = MessagingUtils.CreateSenderParty(activity);
            Activity replyActivity = null;
            bool wasInitiated = false;

            if (IsInitialized()
                && senderParty.ChannelAccount != null
                && !RoutingDataManager.IsAssociatedWithAggregation(senderParty))
            {
                // Sender is not engaged in a conversation and is not a member of the aggregation
                // channel - thus, it must be a new "customer"

                // TODO: The user experience here can be greatly improved by instead of sending
                // a message displaying a card with buttons "accept" and "reject" on it

                string senderName = senderParty.ChannelAccount.Name;
                bool wasSuccessful = false;

                if (AggregationRequired)
                {
                    List<ResourceResponse> resourceResponses = new List<ResourceResponse>();

                    foreach (Party aggregationParty in RoutingDataManager.GetAggregationParties())
                    {
                        string botName = ResolveBotNameInConversation(aggregationParty);
                        string commandKeyword = string.IsNullOrEmpty(botName) ? CommandKeyword : ("@" + botName + " ");

                        resourceResponses.Add(
                            await SendMessageToPartyByBotAsync(aggregationParty,
                                $"User \"{senderName}\" requests a chat; type \"{commandKeyword}accept {senderName}\" to accept"));
                    }

                    foreach (ResourceResponse resourceResponse in resourceResponses)
                    {
                        if (MessagingUtils.WasSuccessful(resourceResponse) && activity is Activity)
                        {
                            wasSuccessful = true;
                            break;
                        }
                    }
                }
                else
                {
                    // No aggregation channel/conversation required
                    wasSuccessful = true;
                }

                if (wasSuccessful)
                {
                    RoutingDataManager.AddPendingRequest(senderParty);
                    replyActivity = (activity as Activity).CreateReply("Please wait for your request to be accepted");
                    wasInitiated = true;
                }
                else
                {
                    replyActivity = (activity as Activity).CreateReply("An error occured processing your request...");
                }
            }

            if (replyActivity != null)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                await connector.Conversations.ReplyToActivityAsync(replyActivity);
            }

            return wasInitiated;
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
        private async Task<ConversationResourceResponse> AddEngagementAsync(Party conversationOwnerParty, Party conversationClientParty)
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
        public async void HandleMessageAsync(Activity activity)
        {
            Party senderParty = MessagingUtils.CreateSenderParty(activity);
            Activity replyActivity = null;

            if (RoutingDataManager.IsEngaged(senderParty, EngagementProfile.Owner))
            {
                // Sender is an owner of an ongoing conversation - forward the message
                Party partyToForwardMessageTo = RoutingDataManager.GetEngagedCounterpart(senderParty);

                if (partyToForwardMessageTo != null)
                {
                    ResourceResponse resourceResponse =
                        await SendMessageToPartyByBotAsync(partyToForwardMessageTo, activity.Text);

                    if (!MessagingUtils.WasSuccessful(resourceResponse))
                    {
                        replyActivity = activity.CreateReply("Failed to send the message");
                    }
                }
            }
            else if (RoutingDataManager.IsEngaged(senderParty, EngagementProfile.Client))
            {
                // Sender is a participant of an ongoing conversation - forward the message
                Party partyToForwardMessageTo = RoutingDataManager.GetEngagedCounterpart(senderParty);

                if (partyToForwardMessageTo != null)
                {
                    string message = $"{senderParty.ChannelAccount.Name} says: {activity.Text}";
                    await SendMessageToPartyByBotAsync(partyToForwardMessageTo, message);
                }
            }
            else if (!IsInitialized())
            {
                // No aggregation channel set up
                string botName = ResolveBotNameInConversation(senderParty);
                string replyMessage = "Not initialized; type \"";
                replyMessage += string.IsNullOrEmpty(botName) ? CommandKeyword : ("@" + botName + " ");
                replyMessage += "init\" to setup the aggregation channel";
                replyActivity = activity.CreateReply(replyMessage);
            }
            else if ((await InitiateEngagementAsync(activity)) == false) // Try to initiate an engagement
            {
                replyActivity = activity.CreateReply("Failed to handle the message");
            }

            if (replyActivity != null)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                await connector.Conversations.ReplyToActivityAsync(replyActivity);
            }
        }

        /// <summary>
        /// Handles the request acceptance and tries to establish 1:1 chat between the two given
        /// parties.
        /// </summary>
        /// <param name="acceptorParty">The party accepting the request.</param>
        /// <param name="partyToAccept">The party to be accepted.</param>
        /// <returns>True, if the accepted request was handled successfully. False otherwise.</returns>
        private async Task<bool> HandleAcceptedRequestAsync(Party acceptorParty, Party partyToAccept)
        {
            bool wasSuccessful = false;

            ConversationResourceResponse conversationResourceResponse =
                await AddEngagementAsync(acceptorParty, partyToAccept);

            if (conversationResourceResponse != null)
            {
                Party botParty = RoutingDataManager.FindBotPartyByChannelAndConversation(
                    acceptorParty.ChannelId, acceptorParty.ConversationAccount);
                ConnectorClient connectorClient = new ConnectorClient(new Uri(acceptorParty.ServiceUrl));

                IMessageActivity messageActivity = Activity.CreateMessageActivity();
                messageActivity.From = botParty.ChannelAccount;
                messageActivity.Recipient = acceptorParty.ChannelAccount;
                messageActivity.Conversation = new ConversationAccount(id: conversationResourceResponse.Id);
                messageActivity.Text = $"Request from user \"{partyToAccept.ChannelAccount.Name}\" accepted, feel free to start the chat";

                ResourceResponse resourceResponse =
                    await connectorClient.Conversations.SendToConversationAsync((Activity)messageActivity);

                if (MessagingUtils.WasSuccessful(resourceResponse))
                {
                    await SendMessageToPartyByBotAsync(partyToAccept,
                        "Your request has been accepted, feel free to start the chat");

                    wasSuccessful = true;
                }
            }

            return wasSuccessful;
        }

        /// <summary>
        /// Handles the direct commands to the bot.
        /// All messages where the bot was mentioned ("@<bot name>) are checked for possible commands.
        /// </summary>
        /// <param name="activity"></param>
        /// <returns>True, if a command was detected and handled. False otherwise.</returns>
        public async Task<bool> HandleDirectCommandToBotAsync(Activity activity)
        {
            bool wasHandled = false;
            Activity replyActivity = null;

            if (WasBotAddressedDirectly(activity)
                || (!string.IsNullOrEmpty(activity.Text) && activity.Text.StartsWith(CommandKeyword)))
            {
                string message = MessagingUtils.StripMentionsFromMessage(activity);
                
                if (message.StartsWith(CommandKeyword))
                {
                    message = message.Remove(0, CommandKeyword.Length);
                }

                string messageInLowerCase = message?.ToLower();

                if (messageInLowerCase.StartsWith("enable aggregation"))
                {
                    AggregationRequired = true;
                    wasHandled = true;
                }
                else if (messageInLowerCase.StartsWith("disable aggregation"))
                {
                    AggregationRequired = false;
                    wasHandled = true;
                }
                else if (messageInLowerCase.StartsWith("init"))
                {
                    // Check if the Aggregation Party already exists
                    Party aggregationParty = new Party(
                        activity.ServiceUrl,
                        activity.ChannelId,
                        /* not a specific user, but a channel/conv */ null,
                        activity.Conversation);

                    if (RoutingDataManager.AddAggregationParty(aggregationParty))
                    {
                        // Establish the sender's channel/conversation as an aggreated one
                        RoutingDataManager.GetAggregationParties().Add(aggregationParty);
                        replyActivity = activity.CreateReply(
                            "This channel/conversation is now where the requests are aggregated");
                    }
                    else
                    {
                        // Aggregation already exists
                        replyActivity = activity.CreateReply(
                            "This channel/conversation is already receiving requests");
                    }

                    wasHandled = true;
                }
                else if (messageInLowerCase.StartsWith("accept"))
                {
                    // Accept conversation request
                    Party senderParty = MessagingUtils.CreateSenderParty(activity);

                    if (RoutingDataManager.IsAssociatedWithAggregation(senderParty))
                    {
                        // The party is associated with the aggregation and has the right to accept
                        Party senderInConversation =
                            RoutingDataManager.FindEngagedPartyByChannel(senderParty.ChannelId, senderParty.ChannelAccount);

                        if (senderInConversation == null || !RoutingDataManager.IsEngaged(senderInConversation, EngagementProfile.Owner))
                        {
                            if (RoutingDataManager.GetPendingRequests().Count > 0)
                            {
                                // The name of the user to accept should be the second word
                                string[] splitMessage = message.Split(' ');

                                if (splitMessage.Count() > 1 && !string.IsNullOrEmpty(splitMessage[1]))
                                {
                                    Party partyToAccept = null;
                                    string errorMessage = "";

                                    try
                                    {
                                        // TODO: Name alone is not enough to ID the right pending request!
                                        partyToAccept = RoutingDataManager.GetPendingRequests().Single(
                                              party => (party.ChannelAccount != null
                                                  && !string.IsNullOrEmpty(party.ChannelAccount.Name)
                                                  && party.ChannelAccount.Name.Equals(splitMessage[1])));
                                    }
                                    catch (InvalidOperationException e)
                                    {
                                        errorMessage = e.Message;
                                    }

                                    if (partyToAccept != null)
                                    {
                                        if (await HandleAcceptedRequestAsync(senderParty, partyToAccept) == false)
                                        {
                                            replyActivity = activity.CreateReply("Failed to accept the request");
                                        }
                                    }
                                    else
                                    {
                                        replyActivity = activity.CreateReply(
                                            $"Could not find a pending request for user {splitMessage[1]}; {errorMessage}");
                                    }
                                }
                                else
                                {
                                    replyActivity = activity.CreateReply("User name missing");
                                }
                            }
                            else
                            {
                                replyActivity = activity.CreateReply("No pending requests");
                            }
                        }
                        else
                        {
                            Party otherParty = RoutingDataManager.GetEngagedCounterpart(senderInConversation);

                            if (otherParty != null)
                            {
                                replyActivity = activity.CreateReply($"You are already engaged in a conversation with {otherParty.ChannelAccount.Name}");
                            }
                            else
                            {
                                replyActivity = activity.CreateReply("An error occured");
                            }
                        }

                        wasHandled = true;
                    }
                }
                else if (messageInLowerCase.StartsWith("close"))
                {
                    // Close the 1:1 conversation
                    Party senderParty = MessagingUtils.CreateSenderParty(activity);
                    Party senderInConversation =
                        RoutingDataManager.FindEngagedPartyByChannel(senderParty.ChannelId, senderParty.ChannelAccount);

                    if (senderInConversation != null && RoutingDataManager.IsEngaged(senderInConversation, EngagementProfile.Owner))
                    {
                        Party otherParty = RoutingDataManager.GetEngagedCounterpart(senderInConversation);

                        if (RoutingDataManager.RemoveEngagement(senderInConversation, EngagementProfile.Owner) > 0)
                        {
                            replyActivity = activity.CreateReply("You are now disengaged from the conversation");

                            // Notify the other party
                            Party botParty = RoutingDataManager.FindBotPartyByChannelAndConversation(
                                otherParty.ChannelId, otherParty.ConversationAccount);
                            IMessageActivity messageActivity = Activity.CreateMessageActivity();
                            messageActivity.From = botParty.ChannelAccount;
                            messageActivity.Recipient = otherParty.ChannelAccount;
                            messageActivity.Conversation = new ConversationAccount(id: otherParty.ConversationAccount.Id);
                            messageActivity.Text = $"{senderInConversation.ChannelAccount.Name} left the conversation";

                            ConnectorClient connectorClientForOther = new ConnectorClient(new Uri(otherParty.ServiceUrl));

                            ResourceResponse resourceResponse =
                                await connectorClientForOther.Conversations.SendToConversationAsync((Activity)messageActivity);
                        }
                        else
                        {
                            replyActivity = activity.CreateReply("An error occured");
                        }
                    }
                    else
                    {
                        replyActivity = activity.CreateReply("No conversation to close found");
                    }

                    wasHandled = true;
                }
                else if (messageInLowerCase.StartsWith("reset"))
                {
                    ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("Clearing all data..."));

                    RoutingDataManager.DeleteAll();

                    wasHandled = true;
                }
                #region Commands for debugging
                else if (messageInLowerCase.StartsWith("list parties"))
                {
                    string replyMessage = string.Empty;
                    string parties = string.Empty;

                    foreach (Party userParty in RoutingDataManager.GetUserParties())
                    {
                        parties += userParty.ToString() + "\n";
                    }

                    if (string.IsNullOrEmpty(parties))
                    {
                        replyMessage = "No user parties;\n";
                    }
                    else
                    {
                        replyMessage = "Users:\n" + parties;
                    }

                    parties = string.Empty;

                    foreach (Party botParty in RoutingDataManager.GetBotParties())
                    {
                        parties += botParty.ToString() + "\n";
                    }

                    if (string.IsNullOrEmpty(parties))
                    {
                        replyMessage += "No bot parties";
                    }
                    else
                    {
                        replyMessage += "Bot:\n" + parties;
                    }

                    replyActivity = activity.CreateReply(replyMessage);
                    wasHandled = true;
                }
                else if (messageInLowerCase.StartsWith("list requests"))
                {
                    string parties = string.Empty;

                    foreach (Party party in RoutingDataManager.GetPendingRequests())
                    {
                        parties += party.ToString() + "\n";
                    }

                    if (parties.Length == 0)
                    {
                        parties = "No pending requests";
                    }

                    replyActivity = activity.CreateReply(parties);
                    wasHandled = true;
                }
                else if (messageInLowerCase.StartsWith("list conversations"))
                {
                    string parties = RoutingDataManager.EngagementsAsString();

                    if (string.IsNullOrEmpty(parties))
                    {
                        replyActivity = activity.CreateReply("No conversations");
                    }
                    else
                    {
                        replyActivity = activity.CreateReply(parties);
                    }

                    wasHandled = true;
                }
                #endregion Commands for debugging
                else
                {
                    replyActivity = activity.CreateReply($"Command \"{messageInLowerCase}\" not recognized");
                }
            }

            if (replyActivity != null)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                await connector.Conversations.ReplyToActivityAsync(replyActivity);
            }

            return wasHandled;
        }
    }
}