using IntermediatorBot.Strings;
using IntermediatorBotSample.MessageRouting;
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
        public const string CommandRemoveAggregationChannel = "unwatch";
        public const string CommandAcceptRequest = "accept";
        public const string CommandRejectRequest = "reject";
        public const string CommandDisconnect = "disconnect";

#if DEBUG // Commands for debugging
        public const string CommandDeleteAllRoutingData = "reset";
        public const string CommandListAllParties = "list parties";
        public const string CommandListPendingRequests = "list requests";
        public const string CommandListConnections = "list conversations";
        public const string CommandListLastMessageRouterResults = "list results";
#endif

        public const string CommandRequestConnection = "human"; // For "customers"
    }

    /// <summary>
    /// Handler for bot commands related to message routing.
    /// </summary>
    public class CommandMessageHandler
    {
        private const string SkypeChannelId = "skype";
        private MessageRouterManager _messageRouterManager;
        private MessageRouterResultHandler _messageRouterResultHandler;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="messageRouterManager">The message router manager.</param>
        /// <param name="messageRouterResultHandler"/>A MessageRouterResultHandler instance for
        /// handling possible routing actions such as accepting a 1:1 conversation connection.</param>
        public CommandMessageHandler(MessageRouterManager messageRouterManager, MessageRouterResultHandler messageRouterResultHandler)
        {
            _messageRouterManager = messageRouterManager;
            _messageRouterResultHandler = messageRouterResultHandler;
        }

        /// <summary>
        /// Resolves the full command string.
        /// </summary>
        /// <param name="botName">The bot name (handle). If null or empty, the basic commmand keyword is used.</param>
        /// <param name="command">The actual command.</param>
        /// <param name="parameters">The command parameters (if any).</param>
        /// <returns>The generated full command string.</returns>
        public static string ResolveFullCommand(string botName, string command, string[] parameters = null)
        {
            if (string.IsNullOrEmpty(command))
            {
                throw new ArgumentNullException("The actual command itself missing");
            }

            string fullCommand = string.Empty;

            if (string.IsNullOrEmpty(botName))
            {
                fullCommand = $"{Commands.CommandKeyword} {command}";
            }
            else
            {
                fullCommand = $"@{botName} {command}";
            }

            if (parameters != null)
            {
                foreach (string parameter in parameters)
                {
                    if (!string.IsNullOrEmpty(parameter))
                    {
                        fullCommand += $" {parameter}";
                    }
                }
            }

            return fullCommand;
        }

        /// <summary>
        /// Resolves the full command string.
        /// </summary>
        /// <param name="messageRouterManager">The message router manager instance.</param>
        /// <param name="activity">An activity used to resolve the bot name (handle), if available.</param>
        /// <param name="command">The actual command.</param>
        /// <param name="parameters">The command parameters (if any).</param>
        /// <returns>The generated full command string.</returns>
        public static string ResolveFullCommand(
            MessageRouterManager messageRouterManager, Activity activity, string command, string[] parameters = null)
        {
            if (activity != null)
            {
                return ResolveFullCommand(
                    messageRouterManager.RoutingDataManager.ResolveBotNameInConversation(
                        MessagingUtils.CreateSenderParty(activity)), command, parameters);
            }

            return ResolveFullCommand(null, command, parameters);
        }

        /// <summary>
        /// Creates a connection (e.g. human agent) request card.
        /// </summary>
        /// <param name="requestorParty">The party who requested a connection.</param>
        /// <param name="botHandle">The name of the bot (optional).</param>
        /// <returns>A newly created request card as an attachment.</returns>
        public static Attachment CreateRequestCard(Party requestorParty, string botHandle = null)
        {
            string requestorUserName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(requestorParty.ChannelAccount.Name);
            string requestorChannelId = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(requestorParty.ChannelId);
            string requestorChannelAccountId = requestorParty.ChannelAccount.Id;

            string acceptCommand = ResolveFullCommand(botHandle, Commands.CommandAcceptRequest, new string[] { requestorChannelAccountId });
            string rejectCommand = ResolveFullCommand(botHandle, Commands.CommandRejectRequest, new string[] { requestorChannelAccountId });

            HeroCard acceptanceCard = new HeroCard()
            {
                Title = ConversationText.ConnectionRequestTitle,
                Subtitle = string.Format(ConversationText.RequestorDetails, requestorUserName, requestorChannelId),
                Text = string.Format(ConversationText.AcceptRejectConnectionHint, acceptCommand, rejectCommand),

                // Use command keyword as some channels support buttons but not @mentions e.g. Webchat
                Buttons = new List<CardAction>()
                {
                    new CardAction()
                    {
                        Title = ConversationText.AcceptButtonTitle,
                        Type = ActionTypes.ImBack,
                        Value = acceptCommand
                    },
                    new CardAction()
                    {
                        Title = ConversationText.RejectButtonTitle,
                        Type = ActionTypes.ImBack,
                        Value = rejectCommand
                    }
                }
            };

            return acceptanceCard.ToAttachment();
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
            
            if (!string.IsNullOrEmpty(activity.Text)
                && (activity.Text.StartsWith($"{Commands.CommandKeyword} "))
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
                        Party aggregationPartyToAdd =
                            new Party(activity.ServiceUrl, activity.ChannelId, null, activity.Conversation);

                        if (_messageRouterManager.RoutingDataManager.AddAggregationParty(aggregationPartyToAdd))
                        {
                            replyActivity = activity.CreateReply(ConversationText.AggregationChannelSet);
                        }
                        else
                        {
                            replyActivity = activity.CreateReply(ConversationText.AggregationChannelAlreadySet);
                        }

                        wasHandled = true;
                        break;

                    case string command when (command.StartsWith(Commands.CommandRemoveAggregationChannel)):
                        // Remove the sender's channel/conversation from the list of aggregation channels
                        if (_messageRouterManager.RoutingDataManager.IsAssociatedWithAggregation(senderParty))
                        {
                            Party aggregationPartyToRemove =
                                new Party(activity.ServiceUrl, activity.ChannelId, null, activity.Conversation);

                            if (_messageRouterManager.RoutingDataManager.RemoveAggregationParty(aggregationPartyToRemove))
                            {
                                replyActivity = activity.CreateReply(ConversationText.AggregationChannelRemoved);
                            }
                            else
                            {
                                replyActivity = activity.CreateReply(ConversationText.FailedToRemoveAggregationChannel);
                            }

                            wasHandled = true;
                        }

                        break;

                    case string command when (command.StartsWith(Commands.CommandAcceptRequest)
                                              || command.StartsWith(Commands.CommandRejectRequest)):
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
#if DEBUG
                        // We shouldn't respond to command attempts by regular users, but I guess
                        // it's okay when debugging
                        else
                        {
                            replyActivity = activity.CreateReply(ConversationText.ConnectionRequestResponseNotAllowed);
                        }
#endif

                        wasHandled = true;
                        break;

                    case string command when (command.StartsWith(Commands.CommandDisconnect)):
                        // End the 1:1 conversation
                        IList<MessageRouterResult> messageRouterResults = _messageRouterManager.Disconnect(senderParty);

                        foreach (MessageRouterResult messageRouterResult in messageRouterResults)
                        {
                            await _messageRouterResultHandler.HandleResultAsync(messageRouterResult);
                        }

                        wasHandled = true;
                        break;


                    #region Implementation of debugging commands
#if DEBUG

                    case string command when (command.StartsWith(Commands.CommandDeleteAllRoutingData)):
                        // DELETE ALL ROUTING DATA
                        await _messageRouterManager.BroadcastMessageToAggregationChannelsAsync(
                            string.Format(ConversationText.DeletingAllDataWithCommandIssuer, senderParty.ChannelAccount?.Name));
                        replyActivity = activity.CreateReply(ConversationText.DeletingAllData);
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
                            replyMessage = $"No user parties{StringAndCharConstants.LineBreak}";
                        }
                        else
                        {
                            replyMessage = $"Users:{StringAndCharConstants.LineBreak}{partiesAsString}";
                        }

                        partiesAsString = PartyListToString(routingDataManager.GetBotParties());

                        if (string.IsNullOrEmpty(partiesAsString))
                        {
                            replyMessage += "No bot parties";
                        }
                        else
                        {
                            replyMessage += $"Bot:{StringAndCharConstants.LineBreak}{partiesAsString}";
                        }

                        replyActivity = activity.CreateReply(replyMessage);
                        wasHandled = true;
                        break;

                    case string command when (command.StartsWith(Commands.CommandListPendingRequests)):
                        // List all pending requests
                        var attachments = new List<Attachment>();
                            
                        foreach (Party party in _messageRouterManager.RoutingDataManager.GetPendingRequests())
                        {
                            attachments.Add(CreateRequestCard(party, activity.Recipient.Name));
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

                    case string command when (command.StartsWith(Commands.CommandListConnections)):
                        // List all connections (conversations)
                        string parties = _messageRouterManager.RoutingDataManager.ConnectionsToString();

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
                        replyActivity = activity.CreateReply(string.Format(ConversationText.CommandNotRecognized, commandMessage));
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
            Party connectedSenderParty = routingDataManager.FindConnectedPartyByChannel(senderParty.ChannelId, senderParty.ChannelAccount);

            if (connectedSenderParty == null || !routingDataManager.IsConnected(senderParty, ConnectionProfile.Owner))
            {
                // The sender (accepter/rejecter) is NOT connected with another party
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
                            errorMessage = string.Format(
                                ConversationText.FailedToFindPendingRequestForUserWithErrorMessage,
                                splitMessage[1], e.Message);
                        }

                        if (partyToAcceptOrReject != null)
                        {
                            MessageRouterResult messageRouterResult = null;

                            if (doAccept)
                            {
                                messageRouterResult = await _messageRouterManager.ConnectAsync(
                                    senderParty,
                                    partyToAcceptOrReject,
                                    !partyToAcceptOrReject.ChannelId.Contains(SkypeChannelId) // Do not try to create direct conversation in Skype
                                );
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
                        errorMessage = ConversationText.UserNameMissing;
                    }
                }
                else
                {
                    errorMessage = ConversationText.NoPendingRequests;
                }
            }
            else
            {
                // The sender (accepter/rejecter) is ALREADY connected with another party
                Party otherParty = routingDataManager.GetConnectedCounterpart(connectedSenderParty);

                if (otherParty != null)
                {
                    errorMessage = string.Format(ConversationText.AlreadyConnectedWithUser, otherParty.ChannelAccount?.Name);
                }
                else
                {
                    errorMessage = ConversationText.ErrorOccured;
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
            string botName = activity.Recipient?.Name;

            HeroCard thumbnailCard = new HeroCard()
            {
                Title = ConversationText.CommandMenuTitle,
                Subtitle = ConversationText.CommandMenuDescription,

                Text = string.Format(
                    ConversationText.CommandMenuInstructions,
                    Commands.CommandKeyword,
                    botName,
                    ResolveFullCommand(botName, Commands.CommandAcceptRequest, new string[] { "<user ID>" })),

                Buttons = new List<CardAction>()
                {
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Commands.CommandAddAggregationChannel),
                        Type = ActionTypes.ImBack,
                        Value = ResolveFullCommand(botName, Commands.CommandAddAggregationChannel)
                    },
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Commands.CommandRemoveAggregationChannel),
                        Type = ActionTypes.ImBack,
                        Value = ResolveFullCommand(botName, Commands.CommandRemoveAggregationChannel)
                    },
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Commands.CommandDisconnect),
                        Type = ActionTypes.ImBack,
                        Value = ResolveFullCommand(botName, Commands.CommandDisconnect)
                    }
#if DEBUG
                    ,
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Commands.CommandDeleteAllRoutingData),
                        Type = ActionTypes.ImBack,
                        Value = ResolveFullCommand(botName, Commands.CommandDeleteAllRoutingData)
                    },
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Commands.CommandListAllParties),
                        Type = ActionTypes.ImBack,
                        Value = ResolveFullCommand(botName, Commands.CommandListAllParties)
                    },
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Commands.CommandListPendingRequests),
                        Type = ActionTypes.ImBack,
                        Value = ResolveFullCommand(botName, Commands.CommandListPendingRequests)
                    },
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Commands.CommandListConnections),
                        Type = ActionTypes.ImBack,
                        Value = ResolveFullCommand(botName, Commands.CommandListConnections)
                    },
                    new CardAction()
                    {
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Commands.CommandListLastMessageRouterResults),
                        Type = ActionTypes.ImBack,
                        Value = ResolveFullCommand(botName, Commands.CommandListLastMessageRouterResults)
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
                    partiesAsString += $"{party.ToString()}{StringAndCharConstants.LineBreak}";
                }
            }

            return partiesAsString;
        }
#endif
    }
}
