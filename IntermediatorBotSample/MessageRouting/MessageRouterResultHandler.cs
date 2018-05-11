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
        /// Handles all message router results.
        /// </summary>
        /// <param name="messageRouterResult">The result to handle.</param>
        public virtual async Task HandleResultAsync(AbstractMessageRouterResult messageRouterResult)
        {
            if (messageRouterResult == null)
            {
                throw new ArgumentNullException($"The given result ({nameof(messageRouterResult)}) is null");
            }

            if (messageRouterResult is ConnectionRequestResult)
            {
                await HandleConnectionRequestResultAsync(messageRouterResult as ConnectionRequestResult);
            }
            else if (messageRouterResult is ConnectionResult)
            {
                await HandleConnectionResultAsync(messageRouterResult as ConnectionResult);
            }
            else if (messageRouterResult is MessageRoutingResult)
            {

            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionRequestResult"></param>
        protected virtual async Task HandleConnectionRequestResultAsync(
            ConnectionRequestResult connectionRequestResult)
        {
            ConnectionRequest connectionRequest = connectionRequestResult?.ConnectionRequest;

            if (connectionRequest == null || connectionRequest.Requestor == null)
            {
                System.Diagnostics.Debug.WriteLine("No client to inform about the connection request result");
                return;
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
                            messageActivity.Recipient = RoutingDataManager.GetChannelAccount(aggregationChannel, out bool isBot);
                            messageActivity.Attachments = new List<Attachment>
                            {
                                CommandCardFactory.CreateRequestCard(
                                    connectionRequest.Requestor,
                                    RoutingDataManager.GetChannelAccount(
                                        botConversationReference, out isBot)?.Name).ToAttachment()
                            };

                            await _messageRouter.SendMessageAsync(aggregationChannel, messageActivity);
                        }
                    }

                    await _messageRouter.SendMessageAsync(
                        connectionRequest.Requestor, Strings.NotifyClientWaitForRequestHandling);
                    break;

                case ConnectionRequestResultType.AlreadyExists:
                    await _messageRouter.SendMessageAsync(
                        connectionRequest.Requestor, Strings.NotifyClientDuplicateRequest);
                    break;

                case ConnectionRequestResultType.Rejected:
                    if (connectionRequestResult.Rejecter != null)
                    {
                        await _messageRouter.SendMessageAsync(
                            connectionRequestResult.Rejecter, Strings.NotifyOwnerRequestRejected);
                    }

                    await _messageRouter.SendMessageAsync(
                        connectionRequest.Requestor, Strings.NotifyClientRequestRejected);
                    break;

                case ConnectionRequestResultType.NotSetup:
                    await _messageRouter.SendMessageAsync(
                        connectionRequest.Requestor, Strings.NoAgentsAvailable);
                    break;

                case ConnectionRequestResultType.Error:
                    if (connectionRequestResult.Rejecter != null)
                    {
                        await _messageRouter.SendMessageAsync(
                            connectionRequestResult.Rejecter,
                            string.Format(Strings.ConnectionRequestResultErrorWithResult, connectionRequestResult.ErrorMessage));
                    }

                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionResult"></param>
        /// <returns></returns>
        protected virtual async Task HandleConnectionResultAsync(ConnectionResult connectionResult)
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

                    break;

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

                    break;

                case ConnectionResultType.Error:
                    if (connection.ConversationReference1 != null)
                    {
                        await _messageRouter.SendMessageAsync(
                            connection.ConversationReference1,
                            string.Format(Strings.ConnectionResultErrorWithResult, connectionResult.ErrorMessage));
                    }

                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageRoutingResult"></param>
        /// <returns></returns>
        protected virtual async Task HandleMessageRoutingResultAsync(
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

                    break;

                default:
                    break;
            }
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
                    RoutingDataManager.GetChannelAccount(conversationReference, out bool isBot);

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
