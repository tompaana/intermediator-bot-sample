using IntermediatorBot.Strings;
using IntermediatorBotSample.MessageRouting;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Underscore.Bot.MessageRouting;
using Underscore.Bot.MessageRouting.DataStore;
using Underscore.Bot.Models;
using Underscore.Bot.Utils;

namespace IntermediatorBotSample.CommandHandling
{
    /// <summary>
    /// Handler for bot commands related to message routing.
    /// </summary>
    public class CommandMessageHandler
    {
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
            Command command = ExtractCommand(activity);

            if (command != null)
            {
                Party senderParty = MessagingUtils.CreateSenderParty(activity);

                switch (command.BaseCommand.ToLower())
                {
                    case string baseCommand when (baseCommand.Equals(Commands.CommandListOptions)):
                        // Present all command options in a card
                        replyActivity = CommandCardFactory.AddCardToActivity(
                                activity.CreateReply(), CommandCardFactory.CreateCommandOptionsCard(activity.Recipient?.Name));
                        wasHandled = true;
                        break;

                    case string baseCommand when (baseCommand.Equals(Commands.CommandAddAggregationChannel)):
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

                    case string baseCommand when (baseCommand.Equals(Commands.CommandRemoveAggregationChannel)):
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

                    case string baseCommand when (baseCommand.Equals(Commands.CommandAcceptRequest)
                                                  || baseCommand.Equals(Commands.CommandRejectRequest)):
                        // Accept/reject conversation request
                        bool doAccept = baseCommand.Equals(Commands.CommandAcceptRequest);

                        if (_messageRouterManager.RoutingDataManager.IsAssociatedWithAggregation(senderParty))
                        {
                            // The party is associated with the aggregation and has the right to accept/reject
                            if (command.Parameters.Count == 0)
                            {
                                replyActivity = activity.CreateReply();

                                IList<Party> pendingRequests =
                                    _messageRouterManager.RoutingDataManager.GetPendingRequests();

                                if (pendingRequests.Count == 0)
                                {
                                    replyActivity.Text = ConversationText.NoPendingRequests;
                                }
                                else
                                {
                                    replyActivity = CommandCardFactory.AddCardToActivity(
                                        replyActivity, CommandCardFactory.CreateAcceptOrRejectCardForMultipleRequests(
                                            pendingRequests, doAccept, activity.Recipient?.Name));
                                }
                            }
                            else if (!doAccept
                                && command.Parameters[0].Equals(Commands.CommandParameterAll))
                            {
                                if (!await new MessageRoutingUtils().RejectAllPendingRequestsAsync(
                                        _messageRouterManager, _messageRouterResultHandler))
                                {
                                    replyActivity = activity.CreateReply();
                                    replyActivity.Text = ConversationText.FailedToRejectPendingRequests;
                                }
                            }
                            else
                            {
                                string errorMessage = await new MessageRoutingUtils().AcceptOrRejectRequestAsync(
                                    _messageRouterManager, _messageRouterResultHandler, senderParty, doAccept, command.Parameters[0]);

                                if (!string.IsNullOrEmpty(errorMessage))
                                {
                                    replyActivity = activity.CreateReply();
                                    replyActivity.Text = errorMessage;
                                }
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

                    case string baseCommand when (baseCommand.Equals(Commands.CommandDisconnect)):
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
                    case string baseCommand when (baseCommand.Equals(Commands.CommandDeleteAllRoutingData)):
                        // DELETE ALL ROUTING DATA
                        replyActivity = activity.CreateReply(ConversationText.DeletingAllData);
                        _messageRouterManager.RoutingDataManager.DeleteAll();
                        wasHandled = true;
                        break;

                    case string baseCommand when (baseCommand.Equals(Commands.CommandList)):
                        bool listAll = command.Parameters.Contains(Commands.CommandParameterAll);
                        replyActivity = activity.CreateReply();
                        string replyMessageText = string.Empty;

                        if (listAll || command.Parameters.Contains(Commands.CommandParameterParties))
                        {
                            // List user and bot parties
                            IRoutingDataManager routingDataManager = _messageRouterManager.RoutingDataManager;
                            string partiesAsString = PartyListToString(routingDataManager.GetUserParties());

                            replyMessageText += string.IsNullOrEmpty(partiesAsString)
                                ? $"{ConversationText.NoUsersStored}{StringAndCharConstants.LineBreak}"
                                : $"{ConversationText.Users}:{StringAndCharConstants.LineBreak}{partiesAsString}{StringAndCharConstants.LineBreak}";

                            partiesAsString = PartyListToString(routingDataManager.GetBotParties());

                            replyMessageText += string.IsNullOrEmpty(partiesAsString)
                                ? $"{ConversationText.NoBotsStored}{StringAndCharConstants.LineBreak}"
                                : $"{ConversationText.Bots}:{StringAndCharConstants.LineBreak}{partiesAsString}{StringAndCharConstants.LineBreak}";

                            wasHandled = true;
                        }

                        if (listAll || command.Parameters.Contains(Commands.CommandParameterRequests))
                        {
                            // List all pending requests
                            IList<Attachment> attachments =
                                CommandCardFactory.CreateMultipleRequestCards(
                                    _messageRouterManager.RoutingDataManager.GetPendingRequests(), activity.Recipient?.Name);

                            if (attachments.Count > 0)
                            {
                                replyMessageText += string.Format(ConversationText.PendingRequestsFoundWithCount, attachments.Count);
                                replyMessageText += StringAndCharConstants.LineBreak;
                                replyActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                replyActivity.Attachments = attachments;
                            }
                            else
                            {
                                replyMessageText += $"{ConversationText.NoPendingRequests}{StringAndCharConstants.LineBreak}";
                            }

                            wasHandled = true;
                        }

                        if (listAll || command.Parameters.Contains(Commands.CommandParameterConnections))
                        {
                            // List all connections (conversations)
                            string connectionsAsString = _messageRouterManager.RoutingDataManager.ConnectionsToString();

                            replyMessageText += string.IsNullOrEmpty(connectionsAsString)
                                ? $"{ConversationText.NoConversations}{StringAndCharConstants.LineBreak}"
                                : $"{connectionsAsString}{StringAndCharConstants.LineBreak}";

                            wasHandled = true;
                        }

                        if (listAll || command.Parameters.Contains(Commands.CommandParameterResults))
                        {
                            // List all logged message router results
                            string resultsAsString = _messageRouterManager.RoutingDataManager.GetLastMessageRouterResults();

                            replyMessageText += string.IsNullOrEmpty(resultsAsString)
                                ? $"{ConversationText.NoResults}{StringAndCharConstants.LineBreak}"
                                : $"{resultsAsString}{StringAndCharConstants.LineBreak}";

                            wasHandled = true;
                        }

                        if (!wasHandled)
                        {
                            replyMessageText = ConversationText.InvalidOrMissingCommandParameter;
                        }

                        replyActivity.Text = replyMessageText;
                        break;
#endif
                    #endregion

                    default:
                        replyActivity = activity.CreateReply(string.Format(ConversationText.CommandNotRecognized, command.BaseCommand));
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
        /// Tries to extract a command from the given message activity.
        /// </summary>
        /// <param name="messageActivity">The message activity possibly containing a command.</param>
        /// <returns>A new created Command instance, if successful.
        /// Null in case the message contained no command.
        /// Note that if the Command instance was created, it will always have a non-null list of
        /// parameters even if its size is zero.
        /// </returns>
        private Command ExtractCommand(IMessageActivity messageActivity)
        {
            Command command = null;
            string messageText = messageActivity?.Text?.Trim();

            if (!string.IsNullOrEmpty(messageText))
            {
                string cleanCommandMessage = null;

                if (messageText.StartsWith($"{Commands.CommandKeyword} "))
                {
                    cleanCommandMessage = messageText.Replace(Commands.CommandKeyword, "").Trim();
                }
                else
                {
                    string botName = messageActivity.Recipient?.Name;

                    if (!string.IsNullOrEmpty(botName))
                    {
                        if (messageText.StartsWith($"@{botName}"))
                        {
                            cleanCommandMessage = messageText.Replace($"@{botName}", "").Trim();
                        }
                        else if (messageText.StartsWith(botName))
                        {
                            cleanCommandMessage = messageText.Replace(botName, "").Trim();
                        }
                    }
                }

                if (!string.IsNullOrEmpty(cleanCommandMessage))
                {
                    string[] splitCommand = cleanCommandMessage.Split(' ');

                    if (splitCommand.Count() == 0)
                    {
                        command = new Command(cleanCommandMessage);
                    }
                    else
                    {
                        command = new Command(splitCommand[0]);
                    }

                    if (splitCommand.Count() > 1)
                    {
                        // Extract the parameters
                        for (int i = 1; i < splitCommand.Length; ++i)
                        {
                            if (!string.IsNullOrEmpty(splitCommand[i]))
                            {
                                command.Parameters.Add(splitCommand[i]);
                            }
                        }
                    }
                }
            }

            return command;
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
        private bool WasBotAddressedDirectly(IMessageActivity messageActivity, bool strict = true)
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
