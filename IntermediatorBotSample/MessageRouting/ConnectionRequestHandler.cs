using IntermediatorBotSample.Strings;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Underscore.Bot.MessageRouting;
using Underscore.Bot.MessageRouting.DataStore;
using Underscore.Bot.Models;

namespace IntermediatorBotSample.MessageRouting
{
    /// <summary>
    /// Contains utility methods for accepting and rejecting connection requests.
    /// </summary>
    public class ConnectionRequestHandler
    {
        private const string ChannelIdEmulator = "emulator";
        private const string ChannelIdFacebook = "facebook";
        private const string ChannelIdSkype = "skype";

        /// <summary>
        /// Do not try to create direct conversations when the owner is on one of these channels
        /// </summary>
        private readonly IList<string> NoDirectConversationsWithChannels = new List<string>()
        {
            ChannelIdEmulator,
            ChannelIdFacebook,
            ChannelIdSkype
        };

        /// <summary>
        /// Tries to accept/reject a pending connection request.
        /// </summary>
        /// <param name="messageRouter">The message router.</param>
        /// <param name="messageRouterResultHandler">The message router result handler.</param>
        /// <param name="sender">The sender party (accepter/rejecter).</param>
        /// <param name="doAccept">If true, will try to accept the request. If false, will reject.</param>
        /// <param name="channelAccountIdOfPartyToAcceptOrReject">The channel account ID of the party whose request to accep/reject.</param>
        /// <returns>Null, if an accept/reject operation was executed successfully.
        /// A user friendly error message otherwise.</returns>
        public async Task<string> AcceptOrRejectRequestAsync(
            MessageRouter messageRouter, MessageRouterResultHandler messageRouterResultHandler,
            ConversationReference sender, bool doAccept, string channelAccountIdOfPartyToAcceptOrReject)
        {
            string errorMessage = null;

            RoutingDataManager routingDataManager = messageRouter.RoutingDataManager;
            ConnectionRequest connectionRequest = null;

            if (routingDataManager.GetConnectionRequests().Count > 0)
            {
                try
                {
                    // Find the connection request based on the channel account ID of the requestor
                    connectionRequest = routingDataManager.GetConnectionRequests().Single(request =>
                            (RoutingDataManager.GetChannelAccount(request.Requestor, out bool isBot) != null
                              && RoutingDataManager.GetChannelAccount(request.Requestor, out isBot).Id
                                .Equals(channelAccountIdOfPartyToAcceptOrReject)));
                }
                catch (InvalidOperationException e)
                {
                    errorMessage = string.Format(
                        ConversationText.FailedToFindPendingRequestForUserWithErrorMessage,
                        channelAccountIdOfPartyToAcceptOrReject,
                        e.Message);
                }
            }

            if (connectionRequest != null)
            {
                Connection connection = routingDataManager.FindConnection(sender);
                ConversationReference senderInConnection = null;
                ConversationReference counterpart = null;

                if (RoutingDataManager.HaveMatchingChannelAccounts(sender, connection.ConversationReference1))
                {
                    senderInConnection = connection.ConversationReference1;
                    counterpart = connection.ConversationReference2;
                }
                else
                {
                    senderInConnection = connection.ConversationReference2;
                    counterpart = connection.ConversationReference1;
                }

                MessageRouterResult messageRouterResult = null;

                if (doAccept)
                {
                    if (senderInConnection != null)
                    {
                        // The sender (accepter/rejecter) is ALREADY connected to another party
                        if (counterpart != null)
                        {
                            errorMessage = string.Format(
                                ConversationText.AlreadyConnectedWithUser,
                                RoutingDataManager.GetChannelAccount(counterpart, out bool isBot)?.Name);
                        }
                        else
                        {
                            errorMessage = ConversationText.ErrorOccured;
                        }
                    }
                    else
                    {
                        bool createNewDirectConversation =
                            !(NoDirectConversationsWithChannels.Contains(sender.ChannelId.ToLower()));

                        // Try to accept
                        messageRouterResult = await messageRouter.ConnectAsync(
                            sender,
                            connectionRequest.Requestor,
                            createNewDirectConversation);
                    }
                }
                else
                {
                    // Note: Rejecting is OK even if the sender is alreay connected
                    messageRouterResult = messageRouter.RejectConnectionRequest(connectionRequest.Requestor, sender);
                }

                if (messageRouterResult != null)
                {
                    await messageRouterResultHandler.HandleResultAsync(messageRouterResult);
                }
            }
            else
            {
                errorMessage = ConversationText.FailedToFindPendingRequest;
            }

            return errorMessage;
        }

        /// <summary>
        /// Tries to reject all pending requests.
        /// </summary>
        /// <param name="messageRouter">The message router.</param>
        /// <param name="messageRouterResultHandler">The message router result handler.</param>
        /// <returns>True, if successful. False otherwise.</returns>
        public async Task<bool> RejectAllPendingRequestsAsync(
            MessageRouter messageRouter, MessageRouterResultHandler messageRouterResultHandler)
        {
            bool wasSuccessful = false;
            IList<ConnectionRequest> connectionRequests = messageRouter.RoutingDataManager.GetConnectionRequests();

            if (connectionRequests.Count > 0)
            {
                IList<MessageRouterResult> messageRouterResults = new List<MessageRouterResult>();

                foreach (ConnectionRequest connectionRequest in connectionRequests)
                {
                    messageRouterResults.Add(messageRouter.RejectConnectionRequest(connectionRequest.Requestor));
                }

                foreach (MessageRouterResult messageRouterResult in messageRouterResults)
                {
                    await messageRouterResultHandler.HandleResultAsync(messageRouterResult);
                }

                wasSuccessful = true;
            }

            return wasSuccessful;
        }
    }
}
