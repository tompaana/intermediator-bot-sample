using IntermediatorBot.Strings;
using IntermediatorBotSample.CommandHandling;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Underscore.Bot.MessageRouting;
using Underscore.Bot.MessageRouting.DataStore;
using Underscore.Bot.Models;
using Underscore.Bot.Utils;

namespace IntermediatorBotSample.MessageRouting
{
    /// <summary>
    /// Handles results from operations executed by the message router mamanger.
    /// </summary>
    public class MessageRouterResultHandler
    {
        private MessageRouterManager _messageRouterManager;

        public MessageRouterResultHandler(MessageRouterManager messageRouterManager)
        {
            _messageRouterManager = messageRouterManager
                ?? throw new ArgumentNullException(
                    $"The message router manager ({nameof(messageRouterManager)}) cannot be null");
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

        #if DEBUG
            _messageRouterManager.RoutingDataManager.AddMessageRouterResult(messageRouterResult);
        #endif

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

            if (messageRouterResult.ConversationOwnerParty != null)
            {
                await _messageRouterManager.SendMessageToPartyByBotAsync(
                    messageRouterResult.ConversationOwnerParty, errorMessage);
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
            await MessagingUtils.ReplyToActivityAsync(messageRouterResult.Activity, messageText);
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

                await MessagingUtils.ReplyToActivityAsync(messageRouterResult.Activity, messageText);
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
                await MessagingUtils.ReplyToActivityAsync(messageRouterResult.Activity, ConversationText.NoAgentsAvailable);
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
            IRoutingDataManager routingDataManager = _messageRouterManager.RoutingDataManager;

            Party conversationOwnerParty = messageRouterResult.ConversationOwnerParty;
            Party conversationClientParty = messageRouterResult.ConversationClientParty;

            string conversationOwnerName =
                string.IsNullOrEmpty(conversationOwnerParty?.ChannelAccount.Name)
                    ? StringAndCharConstants.NoUserNamePlaceholder
                    : conversationOwnerParty?.ChannelAccount.Name;

            string conversationClientName =
                string.IsNullOrEmpty(conversationClientParty?.ChannelAccount.Name)
                    ? StringAndCharConstants.NoUserNamePlaceholder
                    : conversationClientParty?.ChannelAccount.Name;

            string messageToConversationOwner = string.Empty;
            string messageToConversationClient = string.Empty;

            if (messageRouterResult.Type == MessageRouterResultType.ConnectionRequested)
            {
                bool conversationClientPartyMissing =
                    (conversationClientParty == null || conversationClientParty.ChannelAccount == null);

                foreach (Party aggregationParty in _messageRouterManager.RoutingDataManager.GetAggregationParties())
                {
                    Party botParty = routingDataManager.FindBotPartyByChannelAndConversation(
                        aggregationParty.ChannelId, aggregationParty.ConversationAccount);

                    if (botParty != null)
                    {
                        if (conversationClientPartyMissing)
                        {
                            await _messageRouterManager.SendMessageToPartyByBotAsync(
                                aggregationParty, ConversationText.RequestorDetailsMissing);
                        }
                        else
                        {
                            IMessageActivity messageActivity = Activity.CreateMessageActivity();
                            messageActivity.Conversation = aggregationParty.ConversationAccount;
                            messageActivity.Recipient = aggregationParty.ChannelAccount;
                            messageActivity.Attachments = new List<Attachment>
                            {
                                CommandCardFactory.CreateRequestCard(
                                    conversationClientParty, botParty.ChannelAccount?.Name).ToAttachment()
                            };

                            await _messageRouterManager.SendMessageToPartyByBotAsync(
                                aggregationParty, messageActivity);
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

            if (conversationOwnerParty != null
                && !string.IsNullOrEmpty(messageToConversationOwner))
            {
                await _messageRouterManager.SendMessageToPartyByBotAsync(
                    conversationOwnerParty, messageToConversationOwner);
            }

            if (conversationClientParty != null
                && !string.IsNullOrEmpty(messageToConversationClient))
            {
                await _messageRouterManager.SendMessageToPartyByBotAsync(
                    conversationClientParty, messageToConversationClient);
            }
        }
    }
}
