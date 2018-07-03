using IntermediatorBotSample.MessageRouting;
using IntermediatorBotSample.Resources;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
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
        private ConnectionRequestHandler _connectionRequestHandler;
        private IList<string> _permittedAggregationChannels;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="messageRouter">The message router.</param>
        /// <param name="messageRouterResultHandler">A MessageRouterResultHandler instance for
        /// handling possible routing actions such as accepting connection requests.</param>
        /// <param name="connectionRequestHandler">The connection request handler.</param>
        /// <param name="permittedAggregationChannels">Permitted aggregation channels.
        /// Null list means all channels are allowed.</param>
        public CommandHandler(
            MessageRouter messageRouter,
            MessageRouterResultHandler messageRouterResultHandler,
            ConnectionRequestHandler connectionRequestHandler,
            IList<string> permittedAggregationChannels = null)
        {
            _messageRouter = messageRouter;
            _messageRouterResultHandler = messageRouterResultHandler;
            _connectionRequestHandler = connectionRequestHandler;
            _permittedAggregationChannels = permittedAggregationChannels;
        }

        /// <summary>
        /// Checks the given activity for a possible command.
        /// </summary>
        /// <param name="activity">The context containing the activity, which in turn may contain a possible command.</param>
        /// <returns>True, if a command was detected and handled. False otherwise.</returns>
        public async virtual Task<bool> HandleCommandAsync(ITurnContext context)
        {
            Activity activity = context.Activity;
            Command command = Command.FromMessageActivity(activity);

            if (command == null)
            {
                // Check for back channel command
                command = Command.FromChannelData(activity);
            }

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
                    bool isPermittedAggregationChannel = false;

                    if (_permittedAggregationChannels != null && _permittedAggregationChannels.Count > 0)
                    {
                        foreach (string permittedAggregationChannel in _permittedAggregationChannels)
                        {
                            if (!string.IsNullOrWhiteSpace(activity.ChannelId)
                                && activity.ChannelId.ToLower().Equals(permittedAggregationChannel.ToLower()))
                            {
                                isPermittedAggregationChannel = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        isPermittedAggregationChannel = true;
                    }

                    if (isPermittedAggregationChannel)
                    {
                        ConversationReference aggregationChannelToAdd = new ConversationReference(
                            null, null, null,
                            activity.Conversation, activity.ChannelId, activity.ServiceUrl);

                        ModifyRoutingDataResult modifyRoutingDataResult =
                            _messageRouter.RoutingDataManager.AddAggregationChannel(aggregationChannelToAdd);

                        if (modifyRoutingDataResult.Type == ModifyRoutingDataResultType.Added)
                        {
                            replyActivity = activity.CreateReply(Strings.AggregationChannelSet);
                        }
                        else if (modifyRoutingDataResult.Type == ModifyRoutingDataResultType.AlreadyExists)
                        {
                            replyActivity = activity.CreateReply(Strings.AggregationChannelAlreadySet);
                        }
                        else if (modifyRoutingDataResult.Type == ModifyRoutingDataResultType.Error)
                        {
                            replyActivity = activity.CreateReply(
                                string.Format(Strings.FailedToSetAggregationChannel, modifyRoutingDataResult.ErrorMessage));
                        }
                    }
                    else
                    {
                        replyActivity = activity.CreateReply(
                            string.Format(Strings.NotPermittedAggregationChannel, activity.ChannelId));
                    }

                    wasHandled = true;
                    break;

                case Commands.Unwatch:
                    // Remove the sender's channel/conversation from the list of aggregation channels
                    if (_messageRouter.RoutingDataManager.IsAssociatedWithAggregation(sender))
                    {
                        ConversationReference aggregationChannelToRemove = new ConversationReference(
                                null, null, null,
                                activity.Conversation, activity.ChannelId, activity.ServiceUrl);

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

                case Commands.GetRequests:
                    IList<ConnectionRequest> connectionRequests =
                        _messageRouter.RoutingDataManager.GetConnectionRequests();

                    replyActivity = activity.CreateReply();

                    if (connectionRequests.Count == 0)
                    {
                        replyActivity.Text = Strings.NoPendingRequests;
                    }
                    else
                    {
                        replyActivity.Attachments = CommandCardFactory.CreateMultipleConnectionRequestCards(
                            connectionRequests, activity.Recipient?.Name);
                    }

                    replyActivity.ChannelData = JsonConvert.SerializeObject(connectionRequests);
                    wasHandled = true;
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

                            connectionRequests =
                                _messageRouter.RoutingDataManager.GetConnectionRequests();

                            if (connectionRequests.Count == 0)
                            {
                                replyActivity.Text = Strings.NoPendingRequests;
                            }
                            else
                            {
                                replyActivity = CommandCardFactory.AddCardToActivity(
                                    replyActivity, CommandCardFactory.CreateMultiConnectionRequestCard(
                                        connectionRequests, doAccept, activity.Recipient?.Name));
                            }
                        }
                        else if (!doAccept
                            && command.Parameters[0].Equals(Command.CommandParameterAll))
                        {
                            // Reject all pending connection requests
                            if (!await _connectionRequestHandler.RejectAllPendingRequestsAsync(
                                    _messageRouter, _messageRouterResultHandler))
                            {
                                replyActivity = activity.CreateReply();
                                replyActivity.Text = Strings.FailedToRejectPendingRequests;
                            }
                        }
                        else if (command.Parameters.Count > 1)
                        {
                            // Try to accept/reject the specified connection request
                            ChannelAccount requestorChannelAccount =
                                new ChannelAccount(command.Parameters[0]);
                            ConversationAccount requestorConversationAccount =
                                new ConversationAccount(null, null, command.Parameters[1]);

                            AbstractMessageRouterResult messageRouterResult =
                                await _connectionRequestHandler.AcceptOrRejectRequestAsync(
                                    _messageRouter, _messageRouterResultHandler, sender, doAccept,
                                    requestorChannelAccount, requestorConversationAccount);

                            await _messageRouterResultHandler.HandleResultAsync(messageRouterResult);
                        }
                        else
                        {
                            replyActivity = activity.CreateReply(Strings.InvalidOrMissingCommandParameter);
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

                    if (disconnectResults != null && disconnectResults.Count > 0)
                    {
                        foreach (ConnectionResult disconnectResult in disconnectResults)
                        {
                            await _messageRouterResultHandler.HandleResultAsync(disconnectResult);
                        }

                        wasHandled = true;
                    }

                    break;

                default:
                    replyActivity = activity.CreateReply(string.Format(Strings.CommandNotRecognized, command.BaseCommand));
                    break;
            }

            if (replyActivity != null)
            {
                await context.SendActivity(replyActivity);
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
        public bool WasBotAddressedDirectly(IMessageActivity messageActivity, bool strict = true)
        {
            bool botWasMentioned = false;

            if (strict)
            {
                Mention[] mentions = messageActivity.GetMentions();

                foreach (Mention mention in mentions)
                {
                    foreach (ConversationReference bot in _messageRouter.RoutingDataManager.GetBotInstances())
                    {
                        if (mention.Mentioned.Id.Equals(RoutingDataManager.GetChannelAccount(bot).Id))
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
