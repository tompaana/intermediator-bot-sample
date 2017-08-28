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
    public class MessageRouterResultHandler : IMessageRouterResultHandler
    {
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
            WebApiConfig.MessageRouterManager.RoutingDataManager.AddMessageRouterResult(messageRouterResult);
        #endif

            string message = string.Empty;

            switch (messageRouterResult.Type)
            {
                case MessageRouterResultType.NoActionTaken:
                case MessageRouterResultType.OK:
                    // No need to do anything
                    break;
                case MessageRouterResultType.EngagementInitiated:
                case MessageRouterResultType.EngagementAlreadyInitiated:
                case MessageRouterResultType.EngagementRejected:
                case MessageRouterResultType.EngagementAdded:
                case MessageRouterResultType.EngagementRemoved:
                    await HandleEngagementChangedResultAsync(messageRouterResult);
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
        /// Notifies the conversation client (customer) or owner (agent) that an error has occured and
        /// sends the error message
        /// </summary>
        /// <param name="messageRouterResult">The result to handle.</param>
        private static async Task HandleErrorAsync(MessageRouterResult messageRouterResult)
        {
            if (string.IsNullOrEmpty(messageRouterResult.ErrorMessage))
            {
                System.Diagnostics.Debug.WriteLine("An error occured");
            }
            else
            {
                MessageRouterManager messageRouterManager = WebApiConfig.MessageRouterManager;

                foreach (Party aggregationChannel in messageRouterManager.RoutingDataManager.GetAggregationParties())
                {
                    await messageRouterManager.SendMessageToPartyByBotAsync(aggregationChannel, messageRouterResult.ErrorMessage);
                }

                System.Diagnostics.Debug.WriteLine(messageRouterResult.ErrorMessage);
            }
        }

        /// <summary>
        /// Notifies the conversation client (customer) or the conversation owner (agent) that there 
        /// was a problem forwarding their message
        /// </summary>
        /// <param name="messageRouterResult">The result to handle.</param>
        private static async Task<string> HandleFailedToForwardMessageAsync(MessageRouterResult messageRouterResult)
        {
            string message = $"{(string.IsNullOrEmpty(messageRouterResult.ErrorMessage) ? "Failed to forward the message" : messageRouterResult.ErrorMessage)}";
            await MessagingUtils.ReplyToActivityAsync(messageRouterResult.Activity, message);
            return message;
        }

        /// <summary>
        /// Notifies the user that there are no aggregation channels setup 
        /// </summary>
        /// <param name="messageRouterResult">The result to handle.</param>
        private static async Task<string> HandleNoAggregationChannelResultAsync(MessageRouterResult messageRouterResult)
        {
            string message = string.Empty;

            if (messageRouterResult.Activity != null)
            {
                MessageRouterManager messageRouterManager = WebApiConfig.MessageRouterManager;
                string botName = messageRouterManager.RoutingDataManager.ResolveBotNameInConversation(
                    MessagingUtils.CreateSenderParty(messageRouterResult.Activity));

                message = $"{(string.IsNullOrEmpty(messageRouterResult.ErrorMessage) ? "" : $"{messageRouterResult.ErrorMessage}: ")}The message router manager is not initialized; type \"";
                message += string.IsNullOrEmpty(botName) ? $"{Commands.CommandKeyword} " : $"@{botName} ";
                message += $"{Commands.CommandAddAggregationChannel}\" to setup the aggregation channel";

                await MessagingUtils.ReplyToActivityAsync(messageRouterResult.Activity, message);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("The activity of the result is null");
            }

            return message;
        }

        /// <summary>
        /// Notifies the conversation client (customer) that no agents are available 
        /// (i.e. no agents are currently watching for requests
        /// </summary>
        /// <param name="messageRouterResult">The result to handle.</param>
        private static async Task<string> HandleNoAgentsAvailableResultAsync(MessageRouterResult messageRouterResult)
        {
            string message = string.Empty;

            if (messageRouterResult.Activity != null)
            {
                message = $"Sorry. There are no agents available right now.";
                await MessagingUtils.ReplyToActivityAsync(messageRouterResult.Activity, message);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("The activity of the result is null");
            }

            return message;
        }

        /// <summary>
        /// Notifies both the conversation owner (agent) and the conversation client (customer)
        /// about the change in engagement (initiated/started/ended).
        /// </summary>
        /// <param name="messageRouterResult">The result to handle.</param>
        protected virtual async Task HandleEngagementChangedResultAsync(MessageRouterResult messageRouterResult)
        {
            MessageRouterManager messageRouterManager = WebApiConfig.MessageRouterManager;
            IRoutingDataManager routingDataManager = messageRouterManager.RoutingDataManager;

            Party conversationOwnerParty = messageRouterResult.ConversationOwnerParty;
            Party conversationClientParty = messageRouterResult.ConversationClientParty;

            string conversationOwnerName = conversationOwnerParty?.ChannelAccount.Name;
            string conversationClientName = conversationClientParty?.ChannelAccount.Name;

            string messageToConversationOwner = string.Empty;
            string messageToConversationClient = string.Empty;

            if (messageRouterResult.Type == MessageRouterResultType.EngagementInitiated)
            {
                if (conversationClientParty == null || conversationClientParty.ChannelAccount == null)
                {
                    await messageRouterManager.BroadcastMessageToAggregationChannelsAsync("Conversation request was made, but the requester party is null!");
                    throw new NullReferenceException("Conversation request was made, but the requester party is null");
                }

                foreach (Party aggregationParty in messageRouterManager.RoutingDataManager.GetAggregationParties())
                {
                    Party botParty = WebApiConfig.MessageRouterManager.RoutingDataManager
                        .FindBotPartyByChannelAndConversation(aggregationParty.ChannelId, aggregationParty.ConversationAccount);

                    if (botParty != null)
                    {
                        IMessageActivity messageActivity = Activity.CreateMessageActivity();
                        messageActivity.Conversation = aggregationParty.ConversationAccount;
                        messageActivity.Recipient = aggregationParty.ChannelAccount;
                        messageActivity.Attachments = new List<Attachment>
                        {
                            CommandMessageHandler.CreateEngagementRequestHeroCard(conversationClientParty, botParty.ChannelAccount?.Name)
                        };

                        await messageRouterManager.SendMessageToPartyByBotAsync(aggregationParty, messageActivity);
                    }
                    else
                    {
                        await messageRouterManager.BroadcastMessageToAggregationChannelsAsync(
                            $"Could not find the bot party on aggregation channel \"{aggregationParty.ConversationAccount.Name}\"!");
                    }
                }

                messageToConversationClient = "Please wait for your request to be accepted";
            }
            else if (messageRouterResult.Type == MessageRouterResultType.EngagementAlreadyInitiated)
            {
                messageToConversationClient = "Your request has already been receieved and we are waiting for an agent to respond";
            }
            else if (messageRouterResult.Type == MessageRouterResultType.EngagementRejected)
            {
                messageToConversationOwner = $"Request from user \"{conversationClientName}\" rejected";
                messageToConversationClient = "Unfortunately your request could not be processed";
            }
            else if (messageRouterResult.Type == MessageRouterResultType.EngagementAdded)
            {
                messageToConversationOwner = $"You are now connected to user \"{conversationClientName}\"";
                messageToConversationClient = $"Your request was accepted and you are now chatting with {conversationOwnerName}";
            }
            else if (messageRouterResult.Type == MessageRouterResultType.EngagementRemoved)
            {
                messageToConversationOwner = $"You are now disconnected from the conversation with user \"{conversationClientName}\"";
                messageToConversationClient = $"Your conversation with {conversationOwnerName} has ended";
            }

            if (!string.IsNullOrEmpty(messageToConversationOwner) && conversationOwnerParty != null)
            {
                await messageRouterManager.SendMessageToPartyByBotAsync(conversationOwnerParty, messageToConversationOwner);
            }

            if (!string.IsNullOrEmpty(messageToConversationClient) && conversationClientParty != null)
            {
                await messageRouterManager.SendMessageToPartyByBotAsync(conversationClientParty, messageToConversationClient);
            }
        }
    }
}