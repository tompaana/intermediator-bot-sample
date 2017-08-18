using IntermediatorBot.Extensions;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Underscore.Bot.MessageRouting;

namespace IntermediatorBotSample.CommandHandling
{
    public class Commands
    {
        public const string CommandKeyword = "command"; // Used if the channel does not support mentions
        public const string CommandAddAggregationChannel = "watch";
        public const string CommandAcceptRequest = "accept";
        public const string CommandRejectRequest = "reject";
        public const string CommandEndEngagement = "disconnect";
        public const string CommandDeleteAllRoutingData = "reset";

        // For debugging
        public const string CommandListAllParties = "list parties";
        public const string CommandListPendingRequests = "list requests";
        public const string CommandListEngagements = "list conversations";
#if DEBUG
        public const string CommandListLastMessageRouterResults = "list results";
#endif
    }

    /// <summary>
    /// The default handler for bot commands related to message routing.
    /// </summary>
    public class BotCommandHandler
    {
        private IRoutingDataManager _routingDataManager;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="routingDataManager">The routing data manager.</param>
        public BotCommandHandler(IRoutingDataManager routingDataManager)
        {
            _routingDataManager = routingDataManager;
        }

        /// <summary>
        /// All messages where the bot was mentioned ("@<bot name>) are checked for possible commands. If the PermittedAgentChannels
        /// appSetting exists and is populated then commands will only be accepted on included channels in order to prevent normal 
        /// users using commands.
        /// </summary>
        /// <param name="activity">An Activity instance containing a possible command.</param>
        /// <param name="messageRouterManager">The MessageRouterManager instance.</param>
        /// <param name="messageRouterResultHandler"/>A MessageRouterResultHandler instance for
        /// handling possible routing actions such as accepting a 1:1 conversation engagement.</param>
        /// <returns>True, if a command was detected and handled. False otherwise.</returns>
        public async virtual Task<bool> HandleCommandAsync(
            Activity activity, MessageRouterManager messageRouterManager, IMessageRouterResultHandler messageRouterResultHandler)
        {
            bool wasHandled = false;
            Activity replyActivity = null;

            if ((WasBotAddressedDirectly(activity)
                || (!string.IsNullOrEmpty(activity.Text) && activity.Text.StartsWith($"{Commands.CommandKeyword} ")))
                && activity.IsFromPermittedAgentChannel())
            {
                string message = MessagingUtils.StripMentionsFromMessage(activity);

                if (message.StartsWith($"{Commands.CommandKeyword} "))
                {
                    message = message.Remove(0, Commands.CommandKeyword.Length + 1);
                }

                string messageInLowerCase = message?.ToLower();

                if (messageInLowerCase.StartsWith(Commands.CommandAddAggregationChannel))
                {
                    // Check if the Aggregation Party already exists
                    Party aggregationParty = new Party(
                        activity.ServiceUrl,
                        activity.ChannelId,
                        /* not a specific user, but a channel/conv */ null,
                        activity.Conversation);

                    // Establish the sender's channel/conversation as an aggreated one
                    if (_routingDataManager.AddAggregationParty(aggregationParty))
                    {
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
                else if (messageInLowerCase.StartsWith(Commands.CommandAcceptRequest)
                    || messageInLowerCase.StartsWith(Commands.CommandRejectRequest))
                {
                    // Accept/reject conversation request
                    bool doAccept = messageInLowerCase.StartsWith(Commands.CommandAcceptRequest);
                    Party senderParty = MessagingUtils.CreateSenderParty(activity);

                    if (_routingDataManager.IsAssociatedWithAggregation(senderParty))
                    {
                        // The party is associated with the aggregation and has the right to accept/reject
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
                                    Party partyToAcceptOrReject = null;
                                    string errorMessage = "";

                                    try
                                    {
                                        partyToAcceptOrReject = _routingDataManager.GetPendingRequests().Single(
                                              party => (party.ChannelAccount != null
                                                  && !string.IsNullOrEmpty(party.ChannelAccount.Id)
                                                  && party.ChannelAccount.Id.Equals(splitMessage[1])));
                                    }
                                    catch (InvalidOperationException e)
                                    {
                                        errorMessage = e.Message;
                                    }

                                    if (partyToAcceptOrReject != null)
                                    {
                                        MessageRouterResult messageRouterResult = null;

                                        if (doAccept)
                                        {
                                            messageRouterResult = await messageRouterManager.AddEngagementAsync(senderParty, partyToAcceptOrReject);
                                        }
                                        else
                                        {
                                            messageRouterResult = messageRouterManager.RejectPendingRequest(partyToAcceptOrReject, senderParty);
                                        }

                                        await messageRouterResultHandler.HandleResultAsync(messageRouterResult);
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
                else if (messageInLowerCase.StartsWith(Commands.CommandEndEngagement))
                {
                    // End the 1:1 conversation
                    Party senderParty = MessagingUtils.CreateSenderParty(activity);
                    IList<MessageRouterResult> messageRouterResults = messageRouterManager.EndEngagement(senderParty);

                    if (messageRouterResults == null || messageRouterResults.Count == 0)
                    {
                        replyActivity = activity.CreateReply("Failed to end the engagement");
                    }
                    else
                    {
                        foreach (MessageRouterResult messageRouterResult in messageRouterResults)
                        {
                            await messageRouterResultHandler.HandleResultAsync(messageRouterResult);
                        }
                    }

                    wasHandled = true;
                }

                /*
                 * NOTE: Either remove these commands or make them unaccessible should you use this
                 * code in production!
                 */
                #region Commands for debugging
                else if (messageInLowerCase.StartsWith(Commands.CommandDeleteAllRoutingData))
                {
                    ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply("Deleting all data..."));

                    _routingDataManager.DeleteAll();

                    wasHandled = true;
                }
                else if (messageInLowerCase.StartsWith(Commands.CommandListAllParties))
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
                else if (messageInLowerCase.StartsWith(Commands.CommandListPendingRequests))
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
                else if (messageInLowerCase.StartsWith(Commands.CommandListEngagements))
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
#if DEBUG
                else if (messageInLowerCase.StartsWith(Commands.CommandListLastMessageRouterResults))
                {
                    string resultsAsString = messageRouterManager.RoutingDataManager.GetLastMessageRouterResults();
                    replyActivity = activity.CreateReply($"{(string.IsNullOrEmpty(resultsAsString) ? "No results" : resultsAsString)}");
                    wasHandled = true;
                }
#endif
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
