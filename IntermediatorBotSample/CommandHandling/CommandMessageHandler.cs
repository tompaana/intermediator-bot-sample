using IntermediatorBot.Strings;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Underscore.Bot.MessageRouting;
using Underscore.Bot.MessageRouting.DataStore;
using Underscore.Bot.Models;
using Underscore.Bot.Utils;

namespace IntermediatorBotSample.CommandHandling
{
    /// <summary>
    /// Constants for commands.
    /// </summary>
    public struct Commands
    {
        public const string CommandKeyword = "command"; // Used if the channel does not support mentions

        public const string CommandListOptions = "options";
        public const string CommandAddAggregationChannel = "watch";
        public const string CommandAcceptRequest = "accept";
        public const string CommandRejectRequest = "reject";
        public const string CommandEndEngagement = "disconnect";

#if DEBUG // Commands for debugging
        public const string CommandDeleteAllRoutingData = "reset";
        public const string CommandListAllParties = "list parties";
        public const string CommandListPendingRequests = "list requests";
        public const string CommandListEngagements = "list conversations";
        public const string CommandListLastMessageRouterResults = "list results";
#endif
    }

    /// <summary>
    /// Handler for bot commands related to message routing.
    /// </summary>
    public class CommandMessageHandler
    {
        private const string LineBreak = "\n\r";
        private MessageRouterManager _messageRouterManager;
        private IMessageRouterResultHandler _messageRouterResultHandler;

        /// <summary>
        /// Creates a engagement (e.g. human agent) request card.
        /// </summary>
        /// <param name="requesterParty">The party who requested engagement.</param>
        /// <param name="botHandle">The name of the bot.</param>
        /// <returns>A newly created request card as an attachment.</returns>
        public static Attachment CreateEngagementRequestHeroCard(Party requesterParty, string botHandle)
        {
            string requesterUserName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(requesterParty.ChannelAccount.Name);
            string requesterChannelId = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(requesterParty.ChannelId);
            string requesterChannelAccountId = requesterParty.ChannelAccount.Id;

            string commandKeyword = $"{Commands.CommandKeyword}/@{botHandle}";
            string acceptCommand = $"{commandKeyword} {Commands.CommandAcceptRequest} {requesterChannelAccountId}";
            string rejectCommand = $"{commandKeyword} {Commands.CommandRejectRequest} {requesterChannelAccountId}";

            HeroCard acceptanceCard = new HeroCard()
            {
                Title = "Human assistance request",
                Subtitle = $"Requested by \"{requesterUserName}\" ({requesterChannelId})",
                Text = $"Accept or reject the request.\n\nYou can type \"{acceptCommand}\" to accept or \"{rejectCommand}\" to reject, if the buttons are not supported.",

                // Use command keyword as some channels support buttons but not @mentions e.g. Webchat
                Buttons = new List<CardAction>()
                {
                    new CardAction()
                    {
                        Title = "Accept",
                        Type = ActionTypes.PostBack,
                        Value = $"{Commands.CommandKeyword} {Commands.CommandAcceptRequest} {requesterChannelAccountId}"
                    },
                    new CardAction()
                    {
                        Title = "Reject",
                        Type = ActionTypes.PostBack,
                        Value = $"{Commands.CommandKeyword} {Commands.CommandRejectRequest} {requesterChannelAccountId}"
                    }
                }
            };

            return acceptanceCard.ToAttachment();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="messageRouterManager">The message router manager.</param>
        /// <param name="messageRouterResultHandler"/>A MessageRouterResultHandler instance for
        /// handling possible routing actions such as accepting a 1:1 conversation engagement.</param>
        public CommandMessageHandler(MessageRouterManager messageRouterManager, IMessageRouterResultHandler messageRouterResultHandler)
        {
            _messageRouterManager = messageRouterManager;
            _messageRouterResultHandler = messageRouterResultHandler;
        }

        /// <summary>
        /// Checks the given activity for a possible command.
        /// 
        /// All messages that start with a specific command keyword or contain a mention of the bot
        /// ("@<bot name>") are checked for possible commands.
        /// </summary>
        /// <param name="activity">An Activity instance containing a possible command.</param>
        /// <returns>True, if a command was detected and handled. False otherwise.</returns>
        public async virtual Task<bool> HandleCommandAsync(Activity activity)
        {
            bool wasHandled = false;
            Activity replyActivity = null;
            
            if ((!string.IsNullOrEmpty(activity.Text) && activity.Text.StartsWith($"{Commands.CommandKeyword} "))
                || WasBotAddressedDirectly(activity, false))
            {
                string commandMessage = ExtractCleanCommandMessage(activity);
                Party senderParty = MessagingUtils.CreateSenderParty(activity);

                switch (commandMessage.ToLower())
                {
                    case string command when (command.StartsWith(Commands.CommandListOptions)):
                        // Present all command options in a card
                        replyActivity = CreateCommandOptionsCard(activity);
                        wasHandled = true;
                        break;

                    case string command when (command.StartsWith(Commands.CommandAddAggregationChannel)):
                        // Establish the sender's channel/conversation as an aggreated one if not already exists
                        Party aggregationParty = new Party(activity.ServiceUrl, activity.ChannelId, null, activity.Conversation);

                        if (_messageRouterManager.RoutingDataManager.AddAggregationParty(aggregationParty))
                        {
                            replyActivity = activity.CreateReply("This channel/conversation is now where the requests are aggregated");
                        }
                        else
                        {
                            replyActivity = activity.CreateReply("This channel/conversation is already receiving requests");
                        }

                        wasHandled = true;
                        break;

                    case string command when (command.StartsWith(Commands.CommandAcceptRequest) || command.StartsWith(Commands.CommandRejectRequest)):
                        // Accept/reject conversation request
                        bool doAccept = command.StartsWith(Commands.CommandAcceptRequest);

                        if (_messageRouterManager.RoutingDataManager.IsAssociatedWithAggregation(senderParty))
                        {
                            // The party is associated with the aggregation and has the right to accept/reject
                            string errorMessage = await AcceptOrRejectRequestAsync(senderParty, commandMessage, doAccept);
                            
                            if (!string.IsNullOrEmpty(errorMessage))
                            {
                                replyActivity = activity.CreateReply(errorMessage);
                            }
                        }
                        else
                        {
                            replyActivity = activity.CreateReply("Sorry, you are not allowed to accept/reject requests");
                        }

                        wasHandled = true;
                        break;

                    case string command when (command.StartsWith(Commands.CommandEndEngagement)):
                        // End the 1:1 conversation
                        IList<MessageRouterResult> messageRouterResults = _messageRouterManager.EndEngagement(senderParty);

                        if (messageRouterResults == null || messageRouterResults.Count == 0)
                        {
                            replyActivity = activity.CreateReply(ConversationText.CommandEndEngagement);
                        }
                        else
                        {
                            foreach (MessageRouterResult messageRouterResult in messageRouterResults)
                            {
                                await _messageRouterResultHandler.HandleResultAsync(messageRouterResult);
                            }
                        }

                        wasHandled = true;
                        break;


                    #region Implementation of debugging commands
#if DEBUG

                    case string command when (command.StartsWith(Commands.CommandDeleteAllRoutingData)):
                        // DELETE ALL ROUTING DATA
                        await _messageRouterManager.BroadcastMessageToAggregationChannelsAsync(
                            $"Deleting all data as requested by \"{senderParty.ChannelAccount?.Name}\"...");
                        replyActivity = activity.CreateReply("Deleting all data...");
                        _messageRouterManager.RoutingDataManager.DeleteAll();
                        wasHandled = true;
                        break;

                    case string command when (command.StartsWith(Commands.CommandListAllParties)):
                        // List user and bot parties
                        IRoutingDataManager routingDataManager = _messageRouterManager.RoutingDataManager;
                        string replyMessage = string.Empty;
                        string partiesAsString = PartyListToString(routingDataManager.GetUserParties());

                        if (string.IsNullOrEmpty(partiesAsString))
                        {
                            replyMessage = $"No user parties{LineBreak}";
                        }
                        else
                        {
                            replyMessage = $"Users:{LineBreak}{partiesAsString}";
                        }

                        partiesAsString = PartyListToString(routingDataManager.GetBotParties());

                        if (string.IsNullOrEmpty(partiesAsString))
                        {
                            replyMessage += "No bot parties";
                        }
                        else
                        {
                            replyMessage += $"Bot:{LineBreak}{partiesAsString}";
                        }

                        replyActivity = activity.CreateReply(replyMessage);
                        wasHandled = true;
                        break;

                    case string command when (command.StartsWith(Commands.CommandListPendingRequests)):
                        // List all pending requests
                        var attachments = new List<Attachment>();
                            
                        foreach (Party party in _messageRouterManager.RoutingDataManager.GetPendingRequests())
                        {
                            attachments.Add(CreateEngagementRequestHeroCard(party, activity.Recipient.Name));
                        }

                        if (attachments.Count > 0)
                        {
                            replyActivity = activity.CreateReply($"{attachments.Count} pending request(s) found:");
                            replyActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                            replyActivity.Attachments = attachments;
                        }
                        else
                        {
                            replyActivity = activity.CreateReply("No pending requests");
                        }

                        wasHandled = true;
                        break;

                    case string command when (command.StartsWith(Commands.CommandListEngagements)):
                        // List all engagements (conversations)
                        string parties = _messageRouterManager.RoutingDataManager.EngagementsAsString();

                        if (string.IsNullOrEmpty(parties))
                        {
                            replyActivity = activity.CreateReply("No conversations");
                        }
                        else
                        {
                            replyActivity = activity.CreateReply(parties);
                        }

                        wasHandled = true;
                        break;

                    case string command when (command.StartsWith(Commands.CommandListLastMessageRouterResults)):
                        // List all logged message router results
                        string resultsAsString = _messageRouterManager.RoutingDataManager.GetLastMessageRouterResults();
                        replyActivity = activity.CreateReply($"{(string.IsNullOrEmpty(resultsAsString) ? "No results" : resultsAsString)}");
                        wasHandled = true;
                        break;
#endif
                    #endregion

                    default:
                        replyActivity = activity.CreateReply($"Command \"{commandMessage}\" not recognized");
                        break;
                }

                if (replyActivity != null)
                {
                    ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                    await connector.Conversations.ReplyToActivityAsync(replyActivity);
                }
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
        /// <param name="strict">Use false for channels that do not properly support mentions.</param>
        /// <returns>True, if the message was address directly to the bot. False otherwise.</returns>
        protected bool WasBotAddressedDirectly(IMessageActivity messageActivity, bool strict = true)
        {
            bool botWasMentioned = false;

            if (strict)
            {
                Mention[] mentions = messageActivity.GetMentions();

                foreach (Mention mention in mentions)
                {
                    foreach (Party botParty in _messageRouterManager.RoutingDataManager.GetBotParties())
                    {
                        if (mention.Mentioned.Id.Equals(botParty.ChannelAccount.Id))
                        {
                            botWasMentioned = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                // Here we assume the message starts with the bot name, for instance:
                //
                // * "@<BOT NAME>..."
                // * "<BOT NAME>: ..."
                string botName = messageActivity.Recipient?.Name;
                string message = messageActivity.Text?.Trim();

                if (!string.IsNullOrEmpty(botName) && !string.IsNullOrEmpty(message) && message.Length > botName.Length)
                {
                    try
                    {
                        message = message.Remove(botName.Length + 1, message.Length - botName.Length - 1);
                        botWasMentioned = message.Contains(botName);
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to check if bot was mentioned: {e.Message}");
                    }
                }
            }

            return botWasMentioned;
        }

        /// <summary>
        /// Extracts the clean command message from the given activity by stripping the original
        /// message from command keyword or bot mention depending on which one was used.
        /// </summary>
        /// <param name="messageActivity">The message activity containing the message.</param>
        /// <returns>A clean command message.</returns>
        protected string ExtractCleanCommandMessage(IMessageActivity messageActivity)
        {
            string cleanCommandMessage = null;
            string message = messageActivity.Text?.Trim();

            if (!string.IsNullOrEmpty(message))
            {
                if (message.StartsWith($"{Commands.CommandKeyword} "))
                {
                    cleanCommandMessage = message.Replace(Commands.CommandKeyword, "").Trim();
                }
                else
                {
                    string botName = messageActivity.Recipient?.Name;

                    if (message.StartsWith(botName))
                    {
                        cleanCommandMessage = message.Replace(botName, "").Trim();
                    }
                    else if (message.Contains($"@{botName}"))
                    {
                        cleanCommandMessage = message.Replace($"@{botName}", "").Trim();
                    }
                }
            }

            return cleanCommandMessage;
        }

        /// <summary>
        /// Tries to accept/reject a pending request.
        /// </summary>
        /// <param name="senderParty">The sender party (accepter/rejecter).</param>
        /// <param name="commandMessage">The command message. Required for resolving the party to accept/reject.</param>
        /// <param name="doAccept">If true, will try to accept the request. If false, will reject.</param>
        /// <returns>Null, if successful. A user friendly error message otherwise.</returns>
        private async Task<string> AcceptOrRejectRequestAsync(Party senderParty, string commandMessage, bool doAccept)
        {
            string errorMessage = null;
            IRoutingDataManager routingDataManager = _messageRouterManager.RoutingDataManager;
            Party engagedSenderParty = routingDataManager.FindEngagedPartyByChannel(senderParty.ChannelId, senderParty.ChannelAccount);

            if (engagedSenderParty == null || !routingDataManager.IsEngaged(senderParty, EngagementProfile.Owner))
            {
                // The sender (accepter/rejecter) is NOT engaged in a conversation with another party
                if (routingDataManager.GetPendingRequests().Count > 0)
                {
                    // The name of the user to accept should be the second word
                    string[] splitMessage = commandMessage.Split(' ');

                    if (splitMessage.Count() > 1 && !string.IsNullOrEmpty(splitMessage[1]))
                    {
                        Party partyToAcceptOrReject = null;

                        try
                        {
                            partyToAcceptOrReject = routingDataManager.GetPendingRequests().Single(
                                  party => (party.ChannelAccount != null
                                      && !string.IsNullOrEmpty(party.ChannelAccount.Id)
                                      && party.ChannelAccount.Id.Equals(splitMessage[1])));
                        }
                        catch (InvalidOperationException e)
                        {
                            errorMessage = $"Failed to find a pending request for user \"{splitMessage[1]}\": {e.Message}";
                        }

                        if (partyToAcceptOrReject != null)
                        {
                            MessageRouterResult messageRouterResult = null;

                            if (doAccept)
                            {
                                messageRouterResult = await _messageRouterManager.AddEngagementAsync(
                                    senderParty, partyToAcceptOrReject, !partyToAcceptOrReject.ChannelId.Contains("skype"));
                            }
                            else
                            {
                                messageRouterResult = _messageRouterManager.RejectPendingRequest(partyToAcceptOrReject, senderParty);
                            }

                            await _messageRouterResultHandler.HandleResultAsync(messageRouterResult);
                        }
                    }
                    else
                    {
                        errorMessage = "User name missing";
                    }
                }
                else
                {
                    errorMessage = "No pending requests";
                }
            }
            else
            {
                // The sender (accepter/rejecter) is ALREADY engaged in a conversation with another party
                Party otherParty = routingDataManager.GetEngagedCounterpart(engagedSenderParty);

                if (otherParty != null)
                {
                    errorMessage = $"You are already engaged in a conversation with user \"{otherParty.ChannelAccount.Name}\"";
                }
                else
                {
                    errorMessage = "An error occured";
                }
            }

            return errorMessage;
        }

        /// <summary>
        /// Creates a card with all command options.
        /// </summary>
        /// <param name="activity">An activity instance.</param>
        /// <returns>A newly created activity containing the card.</returns>
        private Activity CreateCommandOptionsCard(Activity activity)
        {
            Activity messageActivity = activity.CreateReply();

            HeroCard thumbnailCard = new HeroCard()
            {
                Title = "Command menu",
                Subtitle = "Administrator options for controlling end user bot conversations",
                Text = $"Select from the buttons below **or** type \"{Commands.CommandKeyword}\" or mention the bot (\"@{activity.Recipient.Name}\") followed by the command (e.g. \"{Commands.CommandEndEngagement}\")",
                Buttons = new List<CardAction>()
                {
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Commands.CommandAddAggregationChannel),
                        Type = ActionTypes.PostBack,
                        Value = $"{Commands.CommandKeyword} {Commands.CommandAddAggregationChannel}"
                    },
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Commands.CommandAcceptRequest),
                        Type = ActionTypes.PostBack,
                        Value = $"{Commands.CommandKeyword} {Commands.CommandAcceptRequest} *<request ID>*"
                    },
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Commands.CommandRejectRequest),
                        Type = ActionTypes.PostBack,
                        Value = $"{Commands.CommandKeyword} {Commands.CommandRejectRequest} *<request ID>*"
                    },
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Commands.CommandEndEngagement),
                        Type = ActionTypes.PostBack,
                        Value = $"{Commands.CommandKeyword} {Commands.CommandEndEngagement}"
                    }
#if DEBUG
                    ,
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Commands.CommandDeleteAllRoutingData),
                        Type = ActionTypes.PostBack,
                        Value = $"{Commands.CommandKeyword} {Commands.CommandDeleteAllRoutingData}"
                    },
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Commands.CommandListAllParties),
                        Type = ActionTypes.PostBack,
                        Value = $"{Commands.CommandKeyword} {Commands.CommandListAllParties}"
                    },
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Commands.CommandListPendingRequests),
                        Type = ActionTypes.PostBack,
                        Value = $"{Commands.CommandKeyword} {Commands.CommandListPendingRequests}"
                    },
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Commands.CommandListEngagements),
                        Type = ActionTypes.PostBack,
                        Value = $"{Commands.CommandKeyword} {Commands.CommandListEngagements}"
                    },
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Commands.CommandListLastMessageRouterResults),
                        Type = ActionTypes.PostBack,
                        Value = $"{Commands.CommandKeyword} {Commands.CommandListLastMessageRouterResults}"
                    }
#endif
                }
            };

            messageActivity.Attachments = new List<Attachment>() { thumbnailCard.ToAttachment() };
            return messageActivity;
        }

#if DEBUG
        /// <summary>
        /// For debugging. Creates a string containing all the parties in the given list.
        /// </summary>
        /// <param name="partyList">A list of parties.</param>
        /// <returns>The given parties as string.</returns>
        private string PartyListToString(IList<Party> partyList)
        {
            string partiesAsString = string.Empty;

            if (partyList != null && partyList.Count > 0)
            {
                foreach (Party party in partyList)
                {
                    partiesAsString += $"{party.ToString()}{LineBreak}";
                }
            }

            return partiesAsString;
        }
#endif
    }
}
