using IntermediatorBotSample.Strings;
using IntermediatorBotSample.CommandHandling;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Underscore.Bot.MessageRouting;
using Underscore.Bot.MessageRouting.DataStore;
using Underscore.Bot.Models;
using Underscore.Bot.Utils;
using Microsoft.Bot.Schema;

namespace IntermediatorBotSample.MessageRouting
{
    /// <summary>
    /// Handles results from operations executed by the message router mamanger.
    /// </summary>
    public class MessageRouterResultHandler
    {
        private MessageRouter _messageRouter;

        public MessageRouterResultHandler(MessageRouter messageRouter)
        {
            _messageRouter = messageRouter
                ?? throw new ArgumentNullException(
                    $"The message router manager ({nameof(messageRouter)}) cannot be null");
        }

        /// <summary>
        /// From IMessageRouterResultHandler.
        /// </summary>
        /// <param name="messageRouterResult">The result to handle.</param>
        /// <returns></returns>
        public virtual async Task HandleResultAsync(MessageRouterResult messageRouterResult)
        {
            if (messageRouterResult == null)
            {
                throw new ArgumentNullException($"The given result ({nameof(messageRouterResult)}) is null");
            }

            string message = string.Empty;

            switch (messageRouterResult.Type)
            {
                case MessageRouterResultType.NoActionTaken:
                case MessageRouterResultType.OK:
                    // No need to do anything
                    break;
                case MessageRouterResultType.ConnectionRequested:
                case MessageRouterResultType.ConnectionAlreadyRequested:
                case MessageRouterResultType.ConnectionRejected:
                case MessageRouterResultType.Connected:
                case MessageRouterResultType.Disconnected:
                    await HandleConnectionChangedResultAsync(messageRouterResult);
                    break;
                case MessageRouterResultType.NoAgentsAvailable:
                    await HandleNoAgentsAvailableResultAsync(messageRouterResult);
                    break;
                case MessageRouterResultType.NoAggregationChannel:
                    await HandleNoAggregationChannelResultAsync(messageRouterResult);
                    break;
                case MessageRouterResultType.FailedToForwardMessage:
                    await HandleFailedToForwardMessageAsync(messageRouterResult);
                    break;
                case MessageRouterResultType.Error:
                    await HandleErrorAsync(messageRouterResult);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Tries to notify the conversation owner (agent) about the error.
        /// </summary>
        /// <param name="messageRouterResult">The result to handle.</param>
        protected virtual async Task HandleErrorAsync(MessageRouterResult messageRouterResult)
        {
            string errorMessage = string.IsNullOrEmpty(messageRouterResult.ErrorMessage)
                ? string.Format(ConversationText.ErrorOccuredWithResult, messageRouterResult.Type.ToString())
                : $"{messageRouterResult.ErrorMessage} ({messageRouterResult.Type})";

            System.Diagnostics.Debug.WriteLine(errorMessage);

            if (messageRouterResult.ConversationReferences.Count > 0)
            {
                await _messageRouter.SendMessageAsync(
                    messageRouterResult.ConversationReferences[0], errorMessage);
            }
        }

        /// <summary>
        /// Notifies the conversation client (customer) or the conversation owner (agent) that
        /// there was a problem forwarding their message.
        /// </summary>
        /// <param name="messageRouterResult">The result to handle.</param>
        protected virtual async Task HandleFailedToForwardMessageAsync(MessageRouterResult messageRouterResult)
        {
            string messageText = string.IsNullOrEmpty(messageRouterResult.ErrorMessage)
                ? ConversationText.FailedToForwardMessage
                : messageRouterResult.ErrorMessage;
            await MessageRoutingUtils.ReplyToActivityAsync(messageRouterResult.Activity as Activity, messageText);
        }

        /// <summary>
        /// Notifies the user that there are no aggregation channels set up.
        /// </summary>
        /// <param name="messageRouterResult">The result to handle.</param>
        protected virtual async Task HandleNoAggregationChannelResultAsync(MessageRouterResult messageRouterResult)
        {
            if (messageRouterResult.Activity != null)
            {
                string messageText = string.IsNullOrEmpty(messageRouterResult.ErrorMessage)
                    ? string.Format(ConversationText.NoAggregationChannel)
                    : messageRouterResult.ErrorMessage;
                messageText += $" - ";
                messageText += string.Format(
                    ConversationText.AddAggregationChannelCommandHint,
                    $"{Command.ResolveFullCommand(messageRouterResult.Activity.Recipient?.Name, Commands.CommandAddAggregationChannel)}");

                await MessageRoutingUtils.ReplyToActivityAsync(messageRouterResult.Activity as Activity, messageText);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("The activity of the result is null");
            }
        }

        /// <summary>
        /// Notifies the conversation client (customer) that no agents are available.
        /// </summary>
        /// <param name="messageRouterResult">The result to handle.</param>
        protected virtual async Task HandleNoAgentsAvailableResultAsync(MessageRouterResult messageRouterResult)
        {
            if (messageRouterResult.Activity != null)
            {
                await MessageRoutingUtils.ReplyToActivityAsync(messageRouterResult.Activity as Activity, ConversationText.NoAgentsAvailable);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("The activity of the result is null");
            }
        }

        /// <summary>
        /// Notifies both the conversation owner (agent) and the conversation client (customer)
        /// about the connection status change.
        /// </summary>
        /// <param name="messageRouterResult">The result to handle.</param>
        protected virtual async Task HandleConnectionChangedResultAsync(MessageRouterResult messageRouterResult)
        {
            RoutingDataManager routingDataManager = _messageRouter.RoutingDataManager;

            ConversationReference agent = messageRouterResult.ConversationReferences[0];
            ConversationReference client = messageRouterResult.ConversationReferences[1];
            ChannelAccount agentChannelAccount = MessageRoutingUtils.GetChannelAccount(agent);
            ChannelAccount clientChannelAccount = MessageRoutingUtils.GetChannelAccount(client);

            string conversationOwnerName =
                string.IsNullOrEmpty(agentChannelAccount.Name)
                    ? StringAndCharConstants.NoUserNamePlaceholder
                    : agentChannelAccount.Name;

            string conversationClientName =
                string.IsNullOrEmpty(clientChannelAccount.Name)
                    ? StringAndCharConstants.NoUserNamePlaceholder
                    : clientChannelAccount.Name;

            string messageToConversationOwner = string.Empty;
            string messageToConversationClient = string.Empty;

            if (messageRouterResult.Type == MessageRouterResultType.ConnectionRequested)
            {
                bool conversationClientPartyMissing =
                    (client == null || clientChannelAccount == null);

                foreach (ConversationReference aggregationChannel in _messageRouter.RoutingDataManager.GetAggregationChannels())
                {
                    ConversationReference botConversationReference =
                        routingDataManager.FindBotConversationReferenceByChannelAndConversation(
                            aggregationChannel.ChannelId, aggregationChannel.Conversation);

                    if (botConversationReference != null)
                    {
                        if (conversationClientPartyMissing)
                        {
                            await _messageRouter.SendMessageAsync(
                                aggregationChannel, ConversationText.RequestorDetailsMissing);
                        }
                        else
                        {
                            IMessageActivity messageActivity = Activity.CreateMessageActivity();
                            messageActivity.Conversation = aggregationChannel.Conversation;
                            messageActivity.Recipient = MessageRoutingUtils.GetChannelAccount(aggregationChannel);
                            messageActivity.Attachments = new List<Attachment>
                            {
                                CommandCardFactory.CreateRequestCard(
                                    client, MessageRoutingUtils.GetChannelAccount(botConversationReference)?.Name).ToAttachment()
                            };

                            await _messageRouter.SendMessageAsync(aggregationChannel, messageActivity);
                        }
                    }
                }

                if (!conversationClientPartyMissing)
                {
                    messageToConversationClient = ConversationText.NotifyClientWaitForRequestHandling;
                }
            }
            else if (messageRouterResult.Type == MessageRouterResultType.ConnectionAlreadyRequested)
            {
                messageToConversationClient = ConversationText.NotifyClientDuplicateRequest;
            }
            else if (messageRouterResult.Type == MessageRouterResultType.ConnectionRejected)
            {
                messageToConversationOwner = string.Format(ConversationText.NotifyOwnerRequestRejected, conversationClientName);
                messageToConversationClient = ConversationText.NotifyClientRequestRejected;
            }
            else if (messageRouterResult.Type == MessageRouterResultType.Connected)
            {
                messageToConversationOwner = string.Format(ConversationText.NotifyOwnerConnected, conversationClientName);
                messageToConversationClient = string.Format(ConversationText.NotifyClientConnected, conversationOwnerName);
            }
            else if (messageRouterResult.Type == MessageRouterResultType.Disconnected)
            {
                messageToConversationOwner = string.Format(ConversationText.NotifyOwnerDisconnected, conversationClientName);
                messageToConversationClient = string.Format(ConversationText.NotifyClientDisconnected, conversationOwnerName);
            }

            if (agent != null
                && !string.IsNullOrEmpty(messageToConversationOwner))
            {
                await _messageRouter.SendMessageAsync(agent, messageToConversationOwner);
            }

            if (client != null
                && !string.IsNullOrEmpty(messageToConversationClient))
            {
                await _messageRouter.SendMessageAsync(client, messageToConversationClient);
            }
        }
    }
}
