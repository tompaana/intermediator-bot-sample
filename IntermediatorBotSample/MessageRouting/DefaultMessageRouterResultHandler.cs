using System;
using System.Threading.Tasks;

namespace MessageRouting
{
    public class DefaultMessageRouterResultHandler : IMessageRouterResultHandler
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

            if (messageRouterResult.Type == MessageRouterResultType.OK)
            {
                // No need to do anything
            }
            if (messageRouterResult.Type == MessageRouterResultType.EngagementInitiated
                || messageRouterResult.Type == MessageRouterResultType.EngagementAlreadyInitiated
                || messageRouterResult.Type == MessageRouterResultType.EngagementAdded
                || messageRouterResult.Type == MessageRouterResultType.EngagementRemoved)
            {
                await HandleEngagementChangedResultAsync(messageRouterResult);
            }
            else if (messageRouterResult.Type == MessageRouterResultType.NoAggregationChannel)
            {
                if (messageRouterResult.Activity != null)
                {
                    MessageRouterManager messageRouterManager = MessageRouterManager.Instance;

                    string botName = messageRouterManager.RoutingDataManager.ResolveBotNameInConversation(
                        MessagingUtils.CreateSenderParty(messageRouterResult.Activity));

                    string message = $"{(string.IsNullOrEmpty(messageRouterResult.ErrorMessage)? "" : $"{messageRouterResult.ErrorMessage}: ")}The message router manager is not initialized; type \"";
                    message += string.IsNullOrEmpty(botName) ? $"{Commands.CommandKeyword} " : $"@{botName} ";
                    message += $"{Commands.CommandAddAggregationChannel}\" to setup the aggregation channel";

                    await MessagingUtils.ReplyToActivityAsync(messageRouterResult.Activity, message);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("The activity of the result is null");
                }
            }
            else if (messageRouterResult.Type == MessageRouterResultType.FailedToForwardMessage)
            {
                MessageRouterManager messageRouterManager = MessageRouterManager.Instance;
                string message = $"{(string.IsNullOrEmpty(messageRouterResult.ErrorMessage) ? "Failed to forward the message" : messageRouterResult.ErrorMessage)}";
                await MessagingUtils.ReplyToActivityAsync(messageRouterResult.Activity, message);
            }
            else if (messageRouterResult.Type == MessageRouterResultType.Error)
            {
                System.Diagnostics.Debug.WriteLine($"{(string.IsNullOrEmpty(messageRouterResult.ErrorMessage) ? "An error occured" : messageRouterResult.ErrorMessage)}");
            }
        }

        /// <summary>
        /// Notifies both the conversation owner (agent) and the conversation client (customer)
        /// about the change in engagement (initiated/started/ended).
        /// </summary>
        /// <param name="messageRouterResult">The result to handle.</param>
        protected virtual async Task HandleEngagementChangedResultAsync(MessageRouterResult messageRouterResult)
        {
            MessageRouterManager messageRouterManager = MessageRouterManager.Instance;
            IRoutingDataManager routingDataManager = messageRouterManager.RoutingDataManager;

            Party conversationOwnerParty = messageRouterResult.ConversationOwnerParty;
            Party conversationClientParty = messageRouterResult.ConversationClientParty;

            string conversationOwnerName = conversationOwnerParty?.ChannelAccount.Name;
            string conversationClientName = conversationClientParty?.ChannelAccount.Name;

            string messageToConversationOwner = string.Empty;
            string messageToConversationClient = string.Empty;

            if (messageRouterResult.Type == MessageRouterResultType.EngagementInitiated)
            {
                if (messageRouterManager.AggregationRequired)
                {
                    foreach (Party aggregationParty in messageRouterManager.RoutingDataManager.GetAggregationParties())
                    {
                        string botName = routingDataManager.ResolveBotNameInConversation(aggregationParty);
                        string commandKeyword = string.IsNullOrEmpty(botName)
                            ? Commands.CommandKeyword : $"@{botName}";

                        // TODO: The user experience here can be greatly improved by instead of sending
                        // a message displaying a card with buttons "accept" and "reject" on it
                        await messageRouterManager.SendMessageToPartyByBotAsync(aggregationParty,
                            $"User \"{conversationClientName}\" requests a chat; type \"{commandKeyword} {Commands.CommandAcceptRequest} {conversationClientName}\" to accept");
                    }
                }

                messageToConversationClient = "Please wait for your request to be accepted";
            }
            else if (messageRouterResult.Type == MessageRouterResultType.EngagementAlreadyInitiated)
            {
                messageToConversationClient = "Please wait for your request to be accepted";
            }
            else if (messageRouterResult.Type == MessageRouterResultType.EngagementAdded)
            {
                if (messageRouterManager.AggregationRequired)
                {
                    messageToConversationOwner = $"Request from user \"{conversationClientName}\" accepted, feel free to start the chat";
                    messageToConversationClient = $"Your request has been accepted by {conversationOwnerName}, feel free to start the chat";
                }
                else
                {
                    messageToConversationOwner = $"You are now connected to user \"{conversationClientName}\"";
                    messageToConversationClient = $"You are now chatting with {conversationOwnerName}";
                }
            }
            else if (messageRouterResult.Type == MessageRouterResultType.EngagementRemoved)
            {
                messageToConversationOwner = $"You are now disconnected from the conversation with user \"{conversationClientName}\"";
                messageToConversationClient = $"Your conversation with {conversationOwnerName} has ended";
            }

            if (!string.IsNullOrEmpty(messageToConversationOwner))
            {
                await messageRouterManager.SendMessageToPartyByBotAsync(conversationOwnerParty, messageToConversationOwner);
            }

            if (!string.IsNullOrEmpty(messageToConversationClient))
            {
                await messageRouterManager.SendMessageToPartyByBotAsync(conversationClientParty, messageToConversationClient);
            }
        }
    }
}