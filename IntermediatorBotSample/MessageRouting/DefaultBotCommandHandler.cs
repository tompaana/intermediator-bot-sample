using Microsoft.Bot.Connector;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MessageRouting
{
    /// <summary>
    /// The default handler for bot commands related to message routing.
    /// </summary>
    public class DefaultBotCommandHandler : IBotCommandHandler
    {
        private const string CommandKeyword = "command";
        private const string CommandInitialize = "init";
        private const string CommandAcceptRequest = "accept";
        private const string CommandCloseEngagement = "close";
        private const string CommandDeleteAllRoutingData = "reset";
        private const string CommandEnableAggregation = "enable aggregation";
        private const string CommandDisableAggregation = "disable aggregation";
        private const string CommandListAllParties = "list parties";
        private const string CommandListPendingRequests = "list requests";
        private const string CommandListEngagements = "list conversations";

        private IRoutingDataManager _routingDataManager = MessageRouterManager.Instance.RoutingDataManager;

        public virtual string GetCommandKeyword()
        {
            return CommandKeyword;
        }

        /// <summary>
        /// All messages where the bot was mentioned ("@<bot name>) are checked for possible commands.
        /// See IBotCommandHandler.cs for more information.
        /// </summary>
        public async virtual Task<bool> HandleBotCommandAsync(Activity activity)
        {
            bool wasHandled = false;
            Activity replyActivity = null;

            if (WasBotAddressedDirectly(activity)
                || (!string.IsNullOrEmpty(activity.Text) && activity.Text.StartsWith($"{GetCommandKeyword()} ")))
            {
                string message = MessagingUtils.StripMentionsFromMessage(activity);

                if (message.StartsWith(CommandKeyword))
                {
                    message = message.Remove(0, CommandKeyword.Length);
                }

                string messageInLowerCase = message?.ToLower();

                if (messageInLowerCase.StartsWith(CommandInitialize))
                {
                    if (MessageRouterManager.Instance.AggregationRequired)
                    {
                        // Check if the Aggregation Party already exists
                        Party aggregationParty = new Party(
                            activity.ServiceUrl,
                            activity.ChannelId,
                            /* not a specific user, but a channel/conv */ null,
                            activity.Conversation);

                        if (_routingDataManager.AddAggregationParty(aggregationParty))
                        {
                            // Establish the sender's channel/conversation as an aggreated one
                            _routingDataManager.AddAggregationParty(aggregationParty);
                            replyActivity = activity.CreateReply(
                                "This channel/conversation is now where the requests are aggregated");
                        }
                        else
                        {
                            // Aggregation already exists
                            replyActivity = activity.CreateReply(
                                "This channel/conversation is already receiving requests");
                        }
                    }
                    else
                    {
                        replyActivity = activity.CreateReply("No aggregation in use");
                    }

                    wasHandled = true;
                }
                else if (messageInLowerCase.StartsWith(CommandAcceptRequest))
                {
                    // Accept conversation request
                    Party senderParty = MessagingUtils.CreateSenderParty(activity);

                    if (_routingDataManager.IsAssociatedWithAggregation(senderParty))
                    {
                        // The party is associated with the aggregation and has the right to accept
                        Party senderInConversation =
                            _routingDataManager.FindEngagedPartyByChannel(senderParty.ChannelId, senderParty.ChannelAccount);

                        if (senderInConversation == null || !_routingDataManager.IsEngaged(senderInConversation, EngagementProfile.Owner))
                        {
                            if (_routingDataManager.GetPendingRequests().Count > 0)
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
                                        partyToAccept = _routingDataManager.GetPendingRequests().Single(
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
                                        if (await MessageRouterManager.Instance.HandleAcceptedRequestAsync(senderParty, partyToAccept) == false)
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
                            Party otherParty = _routingDataManager.GetEngagedCounterpart(senderInConversation);

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
                else if (messageInLowerCase.StartsWith(CommandCloseEngagement))
                {
                    // Close the 1:1 conversation
                    Party senderParty = MessagingUtils.CreateSenderParty(activity);
                    Party senderInConversation =
                        _routingDataManager.FindEngagedPartyByChannel(senderParty.ChannelId, senderParty.ChannelAccount);

                    if (senderInConversation != null && _routingDataManager.IsEngaged(senderInConversation, EngagementProfile.Owner))
                    {
                        Party otherParty = _routingDataManager.GetEngagedCounterpart(senderInConversation);

                        if (_routingDataManager.RemoveEngagement(senderInConversation, EngagementProfile.Owner) > 0)
                        {
                            replyActivity = activity.CreateReply("You are now disengaged from the conversation");

                            // Notify the other party
                            Party botParty = _routingDataManager.FindBotPartyByChannelAndConversation(
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

                /*
                 * NOTE: Either remove these commands or make them unaccessible should you use this
                 * code in production!
                 */
                #region Commands for debugging
                else if (messageInLowerCase.StartsWith(CommandEnableAggregation))
                {
                    MessageRouterManager.Instance.AggregationRequired = true;
                    wasHandled = true;
                }
                else if (messageInLowerCase.StartsWith(CommandDisableAggregation))
                {
                    MessageRouterManager.Instance.AggregationRequired = false;
                    wasHandled = true;
                }
                else if (messageInLowerCase.StartsWith(CommandDeleteAllRoutingData))
                {
                    ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("Deleting all data..."));

                    _routingDataManager.DeleteAll();

                    wasHandled = true;
                }
                else if (messageInLowerCase.StartsWith(CommandListAllParties))
                {
                    string replyMessage = string.Empty;
                    string parties = string.Empty;

                    foreach (Party userParty in _routingDataManager.GetUserParties())
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

                    foreach (Party botParty in _routingDataManager.GetBotParties())
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
                else if (messageInLowerCase.StartsWith(CommandListPendingRequests))
                {
                    string parties = string.Empty;

                    foreach (Party party in _routingDataManager.GetPendingRequests())
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
                else if (messageInLowerCase.StartsWith(CommandListEngagements))
                {
                    string parties = _routingDataManager.EngagementsAsString();

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

        /// <summary>
        /// Checks the given activity and determines whether the message was addressed directly to
        /// the bot or not.
        /// 
        /// Note: Only mentions are inspected at the moment.
        /// </summary>
        /// <param name="messageActivity">The message activity.</param>
        /// <returns>True, if the message was address directly to the bot. False otherwise.</returns>
        protected bool WasBotAddressedDirectly(IMessageActivity messageActivity)
        {
            bool botWasMentioned = false;
            Mention[] mentions = messageActivity.GetMentions();

            foreach (Mention mention in mentions)
            {
                foreach (Party botParty in _routingDataManager.GetBotParties())
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
    }
}
