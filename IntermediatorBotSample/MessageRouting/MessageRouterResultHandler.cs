using IntermediatorBotSample.CommandHandling;
using IntermediatorBotSample.Resources;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Underscore.Bot.MessageRouting;
using Underscore.Bot.MessageRouting.DataStore;
using Underscore.Bot.MessageRouting.Models;
using Underscore.Bot.MessageRouting.Results;

namespace IntermediatorBotSample.MessageRouting
{
    /// <summary>
    /// Handles the message router results.
    /// </summary>
    public class MessageRouterResultHandler
    {
        private MessageRouter _messageRouter;

        public MessageRouterResultHandler(MessageRouter messageRouter)
        {
            _messageRouter = messageRouter
                ?? throw new ArgumentNullException(
                    $"({nameof(messageRouter)}) cannot be null");
        }

        /// <summary>
        /// Handles the given message router result.
        /// </summary>
        /// <param name="messageRouterResult">The result to handle.</param>
        /// <returns>True, if the result was handled. False, if no action was taken.</returns>
        public virtual async Task<bool> HandleResultAsync(AbstractMessageRouterResult messageRouterResult)
        {
            if (messageRouterResult != null)
            {
                if (messageRouterResult is ConnectionRequestResult)
                {
                    return await HandleConnectionRequestResultAsync(messageRouterResult as ConnectionRequestResult);
                }

                if (messageRouterResult is ConnectionResult)
                {
                    return await HandleConnectionResultAsync(messageRouterResult as ConnectionResult);
                }

                if (messageRouterResult is MessageRoutingResult)
                {
                    return await HandleMessageRoutingResultAsync(messageRouterResult as MessageRoutingResult);
                }
            }

            return false;
        }

        /// <summary>
        /// Handles the given connection request result.
        /// </summary>
        /// <param name="connectionRequestResult">The result to handle.</param>
        /// <returns>True, if the result was handled. False, if no action was taken.</returns>
        protected virtual async Task<bool> HandleConnectionRequestResultAsync(
            ConnectionRequestResult connectionRequestResult)
        {
            ConnectionRequest connectionRequest = connectionRequestResult?.ConnectionRequest;

            if (connectionRequest == null || connectionRequest.Requestor == null)
            {
                System.Diagnostics.Debug.WriteLine("No client to inform about the connection request result");
                return false;
            }

            switch (connectionRequestResult.Type)
            {
                case ConnectionRequestResultType.Created:
                    foreach (ConversationReference aggregationChannel
                        in _messageRouter.RoutingDataManager.GetAggregationChannels())
                    {
                        ConversationReference botConversationReference =
                            _messageRouter.RoutingDataManager.FindConversationReference(
                                aggregationChannel.ChannelId, aggregationChannel.Conversation.Id, null, true);

                        if (botConversationReference != null)
                        {
                            IMessageActivity messageActivity = Activity.CreateMessageActivity();
                            messageActivity.Conversation = aggregationChannel.Conversation;
                            messageActivity.Recipient = RoutingDataManager.GetChannelAccount(aggregationChannel);
                            messageActivity.Attachments = new List<Attachment>
                            {
                                CommandCardFactory.CreateConnectionRequestCard(
                                    connectionRequest,
                                    RoutingDataManager.GetChannelAccount(
                                        botConversationReference)?.Name).ToAttachment()
                            };

                            await _messageRouter.SendMessageAsync(aggregationChannel, messageActivity);
                        }
                    }

                    await _messageRouter.SendMessageAsync(
                        connectionRequest.Requestor, Strings.NotifyClientWaitForRequestHandling);
                    return true;

                case ConnectionRequestResultType.AlreadyExists:
                    await _messageRouter.SendMessageAsync(
                        connectionRequest.Requestor, Strings.NotifyClientDuplicateRequest);
                    return true;

                case ConnectionRequestResultType.Rejected:
                    if (connectionRequestResult.Rejecter != null)
                    {
                        await _messageRouter.SendMessageAsync(
                            connectionRequestResult.Rejecter,
                            string.Format(Strings.NotifyOwnerRequestRejected, GetNameOrId(connectionRequest.Requestor)));
                    }

                    await _messageRouter.SendMessageAsync(
                        connectionRequest.Requestor, Strings.NotifyClientRequestRejected);
                    return true;

                case ConnectionRequestResultType.NotSetup:
                    await _messageRouter.SendMessageAsync(
                        connectionRequest.Requestor, Strings.NoAgentsAvailable);
                    return true;

                case ConnectionRequestResultType.Error:
                    if (connectionRequestResult.Rejecter != null)
                    {
                        await _messageRouter.SendMessageAsync(
                            connectionRequestResult.Rejecter,
                            string.Format(Strings.ConnectionRequestResultErrorWithResult, connectionRequestResult.ErrorMessage));
                    }

                    return true;

                default:
                    break;
            }

            return false;
        }

        /// <summary>
        /// Handles the given connection result.
        /// </summary>
        /// <param name="connectionResult">The result to handle.</param>
        /// <returns>True, if the result was handled. False, if no action was taken.</returns>
        protected virtual async Task<bool> HandleConnectionResultAsync(ConnectionResult connectionResult)
        {
            Connection connection = connectionResult.Connection;
            
            switch (connectionResult.Type)
            {
                case ConnectionResultType.Connected:
                    if (connection != null)
                    {
                        if (connection.ConversationReference1 != null)
                        {
                            await _messageRouter.SendMessageAsync(
                                connection.ConversationReference1,
                                string.Format(Strings.NotifyOwnerConnected,
                                    GetNameOrId(connection.ConversationReference2)));
                        }

                        if (connection.ConversationReference2 != null)
                        {
                            await _messageRouter.SendMessageAsync(
                                connection.ConversationReference2,
                                string.Format(Strings.NotifyOwnerConnected,
                                    GetNameOrId(connection.ConversationReference1)));
                        }
                    }

                    return true;

                case ConnectionResultType.Disconnected:
                    if (connection != null)
                    {
                        if (connection.ConversationReference1 != null)
                        {
                            await _messageRouter.SendMessageAsync(
                                connection.ConversationReference1,
                                string.Format(Strings.NotifyOwnerDisconnected,
                                    GetNameOrId(connection.ConversationReference2)));
                        }

                        if (connection.ConversationReference2 != null)
                        {
                            await _messageRouter.SendMessageAsync(
                                connection.ConversationReference2,
                                string.Format(Strings.NotifyClientDisconnected,
                                    GetNameOrId(connection.ConversationReference1)));
                        }
                    }

                    return true;

                case ConnectionResultType.Error:
                    if (connection.ConversationReference1 != null)
                    {
                        await _messageRouter.SendMessageAsync(
                            connection.ConversationReference1,
                            string.Format(Strings.ConnectionResultErrorWithResult, connectionResult.ErrorMessage));
                    }

                    return true;

                default:
                    break;
            }

            return false;
        }

        /// <summary>
        /// Handles the given message routing result.
        /// </summary>
        /// <param name="messageRoutingResult">The result to handle.</param>
        /// <returns>True, if the result was handled. False, if no action was taken.</returns>
        protected virtual async Task<bool> HandleMessageRoutingResultAsync(
            MessageRoutingResult messageRoutingResult)
        {
            ConversationReference agent = messageRoutingResult?.Connection?.ConversationReference1;

            switch (messageRoutingResult.Type)
            {
                case MessageRoutingResultType.NoActionTaken:
                case MessageRoutingResultType.MessageRouted:
                    // No need to do anything
                    break;

                case MessageRoutingResultType.FailedToRouteMessage:
                case MessageRoutingResultType.Error:
                    if (agent != null)
                    {
                        string errorMessage = string.IsNullOrWhiteSpace(messageRoutingResult.ErrorMessage)
                            ? Strings.FailedToForwardMessage
                            : messageRoutingResult.ErrorMessage;


                        await _messageRouter.SendMessageAsync(agent, errorMessage);
                    }

                    return true;

                default:
                    break;
            }

            return false;
        }

        /// <summary>
        /// Tries to resolve the name of the given user/bot instance.
        /// Will fallback to ID, if no name specified.
        /// </summary>
        /// <param name="conversationReference">The conversation reference, whose details to resolve.</param>
        /// <returns>The name or the ID of the given user/bot instance.</returns>
        protected virtual string GetNameOrId(ConversationReference conversationReference)
        {
            if (conversationReference != null)
            {
                ChannelAccount channelAccount =
                    RoutingDataManager.GetChannelAccount(conversationReference);

                if (channelAccount != null)
                {
                    if (!string.IsNullOrWhiteSpace(channelAccount.Name))
                    {
                        return channelAccount.Name;
                    }

                    if (!string.IsNullOrWhiteSpace(channelAccount.Id))
                    {
                        return channelAccount.Id;
                    }
                }
            }

            return StringConstants.NoNameOrId;
        }
    }
}
