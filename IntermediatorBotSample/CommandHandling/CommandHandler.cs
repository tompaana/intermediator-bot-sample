using IntermediatorBotSample.MessageRouting;
using IntermediatorBotSample.Resources;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Underscore.Bot.MessageRouting;
using Underscore.Bot.MessageRouting.DataStore;
using Underscore.Bot.MessageRouting.Models;
using Underscore.Bot.MessageRouting.Results;

namespace IntermediatorBotSample.CommandHandling
{
    /// <summary>
    /// Handler for bot commands related to message routing.
    /// </summary>
    public class CommandHandler
    {
        private MessageRouter _messageRouter;
        private MessageRouterResultHandler _messageRouterResultHandler;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="messageRouter">The message router.</param>
        /// <param name="messageRouterResultHandler"/>A MessageRouterResultHandler instance for
        /// handling possible routing actions such as accepting connection requests.</param>
        public CommandHandler(MessageRouter messageRouter, MessageRouterResultHandler messageRouterResultHandler)
        {
            _messageRouter = messageRouter;
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
            Command command = Command.FromMessageActivity(activity);

            if (command == null)
            {
                return false;
            }

            bool wasHandled = false;
            Activity replyActivity = null;
            ConversationReference sender = MessageRouter.CreateSenderConversationReference(activity);

            switch (command.BaseCommand)
            {
                case Commands.ShowOptions:
                    // Present all command options in a card
                    replyActivity = CommandCardFactory.AddCardToActivity(
                            activity.CreateReply(), CommandCardFactory.CreateCommandOptionsCard(activity.Recipient?.Name));
                    wasHandled = true;
                    break;

                case Commands.Watch:
                    // Add the sender's channel/conversation into the list of aggregation channels
                    ConversationReference aggregationChannelToAdd =
                        new ConversationReference(null, null, null, activity.Conversation, activity.ChannelId, activity.ServiceUrl);

                    if (_messageRouter.RoutingDataManager.AddAggregationChannel(aggregationChannelToAdd))
                    {
                        replyActivity = activity.CreateReply(Strings.AggregationChannelSet);
                    }
                    else
                    {
                        replyActivity = activity.CreateReply(Strings.AggregationChannelAlreadySet);
                    }

                    wasHandled = true;
                    break;

                case Commands.Unwatch:
                    // Remove the sender's channel/conversation from the list of aggregation channels
                    if (_messageRouter.RoutingDataManager.IsAssociatedWithAggregation(sender))
                    {
                        ConversationReference aggregationChannelToRemove =
                            new ConversationReference(null, null, null, activity.Conversation, activity.ChannelId, activity.ServiceUrl);

                        if (_messageRouter.RoutingDataManager.RemoveAggregationChannel(aggregationChannelToRemove))
                        {
                            replyActivity = activity.CreateReply(Strings.AggregationChannelRemoved);
                        }
                        else
                        {
                            replyActivity = activity.CreateReply(Strings.FailedToRemoveAggregationChannel);
                        }

                        wasHandled = true;
                    }

                    break;

                case Commands.AcceptRequest:
                case Commands.RejectRequest:
                    // Accept/reject connection request
                    bool doAccept = (command.BaseCommand == Commands.AcceptRequest);

                    if (_messageRouter.RoutingDataManager.IsAssociatedWithAggregation(sender))
                    {
                        // The sender is associated with the aggregation and has the right to accept/reject
                        if (command.Parameters.Count == 0)
                        {
                            replyActivity = activity.CreateReply();

                            IList<ConnectionRequest> connectionRequests =
                                _messageRouter.RoutingDataManager.GetConnectionRequests();

                            if (connectionRequests.Count == 0)
                            {
                                replyActivity.Text = Strings.NoPendingRequests;
                            }
                            else
                            {
                                replyActivity = CommandCardFactory.AddCardToActivity(
                                    replyActivity, CommandCardFactory.CreateAcceptOrRejectCardForMultipleRequests(
                                        connectionRequests, doAccept, activity.Recipient?.Name));
                            }
                        }
                        else if (!doAccept
                            && command.Parameters[0].Equals(Command.CommandParameterAll))
                        {
                            if (!await new ConnectionRequestHandler().RejectAllPendingRequestsAsync(
                                    _messageRouter, _messageRouterResultHandler))
                            {
                                replyActivity = activity.CreateReply();
                                replyActivity.Text = Strings.FailedToRejectPendingRequests;
                            }
                        }
                        else
                        {
                            string errorMessage = await new ConnectionRequestHandler().AcceptOrRejectRequestAsync(
                                _messageRouter, _messageRouterResultHandler, sender, doAccept, command.Parameters[0]);

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
                        replyActivity = activity.CreateReply(Strings.ConnectionRequestResponseNotAllowed);
                    }
#endif

                    wasHandled = true;
                    break;

                case Commands.Disconnect:
                    // End the 1:1 conversation(s)
                    IList<ConnectionResult> disconnectResults = _messageRouter.Disconnect(sender);

                    foreach (ConnectionResult disconnectResult in disconnectResults)
                    {
                        await _messageRouterResultHandler.HandleResultAsync(disconnectResult);
                    }

                    wasHandled = true;
                    break;

                default:
                    replyActivity = activity.CreateReply(string.Format(Strings.CommandNotRecognized, command.BaseCommand));
                    break;
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
                    foreach (ConversationReference bot in _messageRouter.RoutingDataManager.GetBotInstances())
                    {
                        if (mention.Mentioned.Id.Equals(RoutingDataManager.GetChannelAccount(bot, out bool isBot).Id))
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
    }
}
