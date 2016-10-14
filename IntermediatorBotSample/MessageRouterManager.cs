using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IntermediatorBotSample
{
    public class MessageRouterManager
    {
        private static MessageRouterManager _instance;
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

        public IList<Party> UserParties
        {
            get;
            private set;
        }

        /// <summary>
        /// If the bot is addressed from different channels, its identity in terms of ID and name
        /// can vary. Those different identities are stored in this list.
        /// </summary>
        public IList<Party> BotParties
        {
            get;
            private set;
        }

        /// <summary>
        /// Contains 1:1 associations between parties i.e. parties engaged in a conversation.
        /// Furthermore, the key party is considered to be the conversation owner e.g. in
        /// a customer service situation the customer service agent.
        /// </summary>
        public Dictionary<Party, Party> EngagedParties
        {
            get;
            private set;
        }

        /// <summary>
        /// The list of parties waiting for their (conversation) requests to be accepted.
        /// </summary>
        public IList<Party> PendingRequests
        {
            get;
            private set;
        }

        /// <summary>
        /// Represents the channel (and the specific conversation e.g. specific channel in Slack),
        /// where the chat requests are directed. For instance, this channel could be where the
        /// customer service agents accept customer chat requests.
        /// </summary>
        public Party AggregationParty
        {
            get;
            private set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        private MessageRouterManager()
        {
            UserParties = new List<Party>();
            BotParties = new List<Party>();
            PendingRequests = new List<Party>();
            EngagedParties = new Dictionary<Party, Party>();
        }

        /// <summary>
        /// Adds the given party to the container.
        /// </summary>
        /// <param name="newParty">The new party to add.</param>
        /// <param name="isUser">If true, will try to add the party to the list of users.
        /// If false, will try to add it to the list of bot identities.</param>
        /// <returns>True, if the given party was added. False otherwise (was null or already in the collection).</returns>
        public bool AddParty(Party newParty, bool isUser = true)
        {
            if (newParty == null || (isUser ? UserParties.Contains(newParty) : BotParties.Contains(newParty)))
            {
                return false;
            }

            if (isUser)
            {
                UserParties.Add(newParty);
            }
            else
            {
                if (newParty.ChannelAccount == null)
                {
                    throw new NullReferenceException("Channel account of a bot party cannot be null - " + nameof(newParty.ChannelAccount));
                }

                BotParties.Add(newParty);
            }

            return true;
        }

        /// <summary>
        /// Adds a new party with the given properties into the container.
        /// </summary>
        /// <param name="serviceUrl"></param>
        /// <param name="channelId"></param>
        /// <param name="channelAccount"></param>
        /// <param name="conversationAccount"></param>
        /// <param name="isUser">If true, will try to add the party to the list of users.
        /// If false, will try to add it to the list of bot identities.</param>
        /// <returns>True, if the party was added. False otherwise (identical party already in
        /// the collection.</returns>
        public bool AddParty(string serviceUrl, string channelId,
            ChannelAccount channelAccount, ConversationAccount conversationAccount,
            bool isUser = true)
        {
            Party newParty = new Party(serviceUrl, channelId, channelAccount, conversationAccount);
            return AddParty(newParty, isUser);
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
            AddParty(recipientParty, false);

            // Check that the party who sent the message is not the bot
            if (!BotParties.Contains(senderParty))
            {
                // Store the user party, if not already stored
                AddParty(senderParty);
            }
        }

        /// <summary>
        /// Checks the given activity for new parties and adds them to the collection, if not
        /// already there.
        /// </summary>
        /// <param name="activity">The activity.</param>
        public void MakeSurePartiesAreTracked(Activity activity)
        {
            MakeSurePartiesAreTracked(
                MessagingUtils.CreateSenderParty(activity),
                MessagingUtils.CreateRecipientParty(activity));
        }

        /// <summary>
        /// Tries to find a stored bot party instance matching the given channel ID and
        /// conversation account.
        /// </summary>
        /// <param name="channelId">The channel ID.</param>
        /// <param name="conversationAccount">The conversation account.</param>
        /// <returns>The bot party instance matching the given details or null if not found.</returns>
        public Party FindBotPartyByChannelAndConversation(string channelId, ConversationAccount conversationAccount)
        {
            Party botParty = null;

            try
            {
                botParty = BotParties.Single(party =>
                        (party.ChannelId.Equals(channelId)
                         && party.ConversationAccount.Id.Equals(conversationAccount.Id)));
            }
            catch (InvalidOperationException)
            {
            }

            return botParty;
        }

        /// <summary>
        /// Checks if the given party is associated with aggregation. In human toung this means
        /// that the given party is, for instance, a customer service agent who deals with the
        /// requests coming from customers.
        /// </summary>
        /// <param name="party">The party to check.</param>
        /// <returns>True, if is associated. False otherwise.</returns>
        public bool IsAssociatedWithAggregation(Party party)
        {
            return (AggregationParty != null && party != null
                && AggregationParty.IsPartOfSameConversation(party));
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
                foreach (Party botParty in BotParties)
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
        public async Task<APIResponse> SendMessageToPartyByBotAsync(Party partyToMessage, string messageText)
        {
            // We need the channel account of the bot in the SAME CHANNEL as the RECIPIENT.
            // The identity of the bot in the channel of the sender is most likely a different one and
            // thus unusable since it will not be recognized on the recipient's channel.
            Party botParty = FindBotPartyByChannelAndConversation(
                partyToMessage.ChannelId, partyToMessage.ConversationAccount);

            MessagingUtils.ConnectorClientAndMessageBundle bundle =
                MessagingUtils.CreateConnectorClientAndMessageActivity(
                    partyToMessage, messageText, botParty?.ChannelAccount);

            return await bundle.connectorClient.Conversations.SendToConversationAsync(
                (Activity)bundle.messageActivity);
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

            if (EngagedParties.Keys.Contains(senderParty))
            {
                // Sender is an owner of an ongoing conversation - forward the message
                Party partyToForwardMessageTo = null;
                EngagedParties.TryGetValue(senderParty, out partyToForwardMessageTo);

                if (partyToForwardMessageTo != null)
                {
                    APIResponse apiResponse =
                        await SendMessageToPartyByBotAsync(partyToForwardMessageTo, activity.Text);

                    if (!MessagingUtils.WasSuccessful(apiResponse))
                    {
                        replyActivity = activity.CreateReply("Failed to send the message");
                    }
                }
            }
            else if (EngagedParties.Values.Contains(senderParty))
            {
                // Sender is a participant of an ongoing conversation - forward the message
                Party partyToForwardMessageTo = null;
                
                for (int i = 0; i < EngagedParties.Count; ++i)
                {
                    if (EngagedParties.Values.ElementAt(i).Equals(senderParty))
                    {
                        partyToForwardMessageTo = EngagedParties.Keys.ElementAt(i);
                        break;
                    }
                }

                if (partyToForwardMessageTo != null)
                {
                    string message = $"{senderParty.ChannelAccount.Name} says: {activity.Text}";
                    await SendMessageToPartyByBotAsync(partyToForwardMessageTo, message);
                }
            }
            else if (AggregationParty != null
                && senderParty.ChannelAccount != null
                && !IsAssociatedWithAggregation(senderParty))
            {
                // Sender is not engaged in a conversation and is not a member of the aggregation
                // channel - thus, it must be a new "customer"

                // TODO: The user experience here can be greatly improved by instead of sending
                // a message displaying a card with buttons "accept" and "reject" on it

                string senderName = senderParty.ChannelAccount.Name;

                APIResponse apiResponse =
                    await SendMessageToPartyByBotAsync(AggregationParty,
                        $"User {senderName} requests a chat; issue command \"accept {senderName}\" to accept");

                if (MessagingUtils.WasSuccessful(apiResponse))
                {
                    if (!PendingRequests.Contains(senderParty))
                    {
                        PendingRequests.Add(senderParty);
                    }

                    replyActivity = activity.CreateReply("Please wait for your request to be accepted...");
                }
            }
            else if (AggregationParty == null)
            {
                // No aggregation channel set up
                replyActivity = activity.CreateReply(
                    "Not initialized; issue command \"init\" to setup the aggregation channel");
            }

            if (replyActivity != null)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                await connector.Conversations.ReplyToActivityAsync(replyActivity);
            }
        }

        /// <summary>
        /// Handles the direct commands to the bot.
        /// </summary>
        /// <param name="activity"></param>
        /// <returns>True, if a command was detected and handled. False otherwise.</returns>
        public async Task<bool> HandleDirectCommandToBotAsync(Activity activity)
        {
            bool wasHandled = false;
            Activity replyActivity = null;

            if (WasBotAddressedDirectly(activity))
            {
                string message = MessagingUtils.StripMentionsFromMessage(activity);
                string messageInLowerCase = message?.ToLower();

                if (messageInLowerCase.StartsWith("init"))
                {
                    // Establish the sender's channel/conversation as the aggreated one
                    AggregationParty = new Party(
                        activity.ServiceUrl,
                        activity.ChannelId,
                        /* not a specific user, but a channel/conv */ null,
                        activity.Conversation);

                    replyActivity = activity.CreateReply(
                        "This channel/conversation is now where the requests are aggregated");

                    wasHandled = true;
                }
                else if (messageInLowerCase.StartsWith("accept"))
                {
                    // Accept conversation request
                    Party senderParty = MessagingUtils.CreateSenderParty(activity);

                    if (IsAssociatedWithAggregation(senderParty))
                    {
                        // The party is associated with the aggregation and has the right to accept

                        if (PendingRequests.Count > 0)
                        {
                            // The name of the user to accept should be the second word
                            string[] splitMessage = message.Split(' ');

                            if (splitMessage.Count() > 1 && !string.IsNullOrEmpty(splitMessage[1]))
                            {
                                Party partyToAccept = null;
                                string errorMessage = "";

                                try
                                {
                                    partyToAccept = PendingRequests.Single(
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
                }
                else if (messageInLowerCase.StartsWith("list parties"))
                {
                    string parties = "Users:\n";

                    foreach (Party userParty in UserParties)
                    {
                        parties += userParty.ToString() + "\n";
                    }

                    parties += "Bot:\n";

                    foreach (Party botParty in BotParties)
                    {
                        parties += botParty.ToString() + "\n";
                    }

                    replyActivity = activity.CreateReply(parties);
                    wasHandled = true;
                }
                else if (messageInLowerCase.StartsWith("list requests"))
                {
                    string parties = "";

                    foreach (Party party in PendingRequests)
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

        /// <summary>
        /// Handles the request acceptance and tries to establish 1:1 chat between the two given
        /// parties. Note that the acceptor will have a new separate party for the created chat.
        /// </summary>
        /// <param name="acceptorParty">The party accepting the request.</param>
        /// <param name="partyToAccept">The party to be accepted.</param>
        /// <returns>True, if the accepted request was handled successfully. False otherwise.</returns>
        private async Task<bool> HandleAcceptedRequestAsync(Party acceptorParty, Party partyToAccept)
        {
            bool wasSuccessful = false;

            if (acceptorParty == null || partyToAccept == null)
            {
                throw new ArgumentNullException(
                    $"Neither of the arguments ({nameof(acceptorParty)}, {nameof(partyToAccept)}) can be null");
            }

            ConnectorClient connectorClient = new ConnectorClient(new Uri(acceptorParty.ServiceUrl));

            Party botParty = FindBotPartyByChannelAndConversation(
                acceptorParty.ChannelId, acceptorParty.ConversationAccount);

            ResourceResponse conversationId =
                await connectorClient.Conversations.CreateDirectConversationAsync(
                    botParty.ChannelAccount, acceptorParty.ChannelAccount);

            IMessageActivity messageActivity = Activity.CreateMessageActivity();
            messageActivity.From = botParty.ChannelAccount;
            messageActivity.Recipient = acceptorParty.ChannelAccount;
            messageActivity.Conversation = new ConversationAccount(id: conversationId.Id);
            messageActivity.Text = $"Request from user {partyToAccept.ChannelAccount.Name} accepted, feel free to start the chat";

            APIResponse apiResponse =
                await connectorClient.Conversations.SendToConversationAsync((Activity)messageActivity);

            if (MessagingUtils.WasSuccessful(apiResponse))
            {
                // The conversation account of the conversation owner for this 1:1 chat is different -
                // thus, we need to create a new party instance
                Party acceptorPartyEngaged = new Party(
                    acceptorParty.ServiceUrl, acceptorParty.ChannelId,
                    acceptorParty.ChannelAccount, messageActivity.Conversation);

                AddParty(acceptorPartyEngaged);
                EngagedParties.Add(acceptorPartyEngaged, partyToAccept);
                PendingRequests.Remove(partyToAccept);

                await SendMessageToPartyByBotAsync(partyToAccept,
                    "Your request has been accepted, feel free to start the chat");

                wasSuccessful = true;
            }

            return wasSuccessful;
        }
    }
}