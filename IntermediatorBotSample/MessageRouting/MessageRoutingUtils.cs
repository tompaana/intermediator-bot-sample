using IntermediatorBot.Strings;
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
    /// Contains message routing related utility methods.
    /// </summary>
    public class MessageRoutingUtils
    {
        private const string EmulatorChannelId = "emulator";
        private const string SkypeChannelId = "skype";

        /// <summary>
        /// Tries to accept/reject a pending request.
        /// </summary>
        /// <param name="messageRouterManager">The message router manager.</param>
        /// <param name="messageRouterResultHandler">The message router result handler.</param>
        /// <param name="senderParty">The sender party (accepter/rejecter).</param>
        /// <param name="doAccept">If true, will try to accept the request. If false, will reject.</param>
        /// <param name="channelAccountIdOfPartyToAcceptOrReject">The channel account ID of the party whose request to accep/reject.</param>
        /// <returns>Null, if an accept/reject operation was executed successfully.
        /// A user friendly error message otherwise.</returns>
        public async Task<string> AcceptOrRejectRequestAsync(
            MessageRouterManager messageRouterManager, MessageRouterResultHandler messageRouterResultHandler,
            Party senderParty, bool doAccept, string channelAccountIdOfPartyToAcceptOrReject)
        {
            string errorMessage = null;

            IRoutingDataManager routingDataManager = messageRouterManager.RoutingDataManager;
            Party partyToAcceptOrReject = null;

            if (routingDataManager.GetPendingRequests().Count > 0)
            {
                try
                {
                    partyToAcceptOrReject = routingDataManager.GetPendingRequests().Single(
                          party => (party.ChannelAccount != null
                              && !string.IsNullOrEmpty(party.ChannelAccount.Id)
                              && party.ChannelAccount.Id.Equals(channelAccountIdOfPartyToAcceptOrReject)));
                }
                catch (InvalidOperationException e)
                {
                    errorMessage = string.Format(
                        ConversationText.FailedToFindPendingRequestForUserWithErrorMessage,
                        channelAccountIdOfPartyToAcceptOrReject,
                        e.Message);
                }
            }

            if (partyToAcceptOrReject != null)
            {
                Party connectedSenderParty =
                routingDataManager.FindConnectedPartyByChannel(
                    senderParty.ChannelId, senderParty.ChannelAccount);

                bool senderIsConnected =
                    (connectedSenderParty != null
                    && routingDataManager.IsConnected(connectedSenderParty, ConnectionProfile.Owner));

                MessageRouterResult messageRouterResult = null;

                if (doAccept)
                {
                    if (senderIsConnected)
                    {
                        // The sender (accepter/rejecter) is ALREADY connected with another party
                        Party otherParty = routingDataManager.GetConnectedCounterpart(connectedSenderParty);

                        if (otherParty != null)
                        {
                            errorMessage = string.Format(
                                ConversationText.AlreadyConnectedWithUser, otherParty.ChannelAccount?.Name);
                        }
                        else
                        {
                            errorMessage = ConversationText.ErrorOccured;
                        }
                    }
                    else
                    {
                        // Do not try to create direct conversation in emulator or Skype 
                        bool createNewDirectConversation =
                            !(senderParty.ChannelId.Contains(SkypeChannelId)
                              || senderParty.ChannelId.Contains(EmulatorChannelId));

                        // Try to accept
                        messageRouterResult = await messageRouterManager.ConnectAsync(
                            senderParty,
                            partyToAcceptOrReject,
                            createNewDirectConversation);

                        await messageRouterResultHandler.HandleResultAsync(messageRouterResult);
                    }
                }
                else
                {
                    // Note: Rejecting is OK even if the sender is alreay connected
                    messageRouterResult = messageRouterManager.RejectPendingRequest(partyToAcceptOrReject, senderParty);
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
        /// <param name="messageRouterManager">The message router manager.</param>
        /// <param name="messageRouterResultHandler">The message router result handler.</param>
        /// <returns>True, if successful. False otherwise.</returns>
        public async Task<bool> RejectAllPendingRequestsAsync(
            MessageRouterManager messageRouterManager, MessageRouterResultHandler messageRouterResultHandler)
        {
            bool wasSuccessful = false;
            IList<Party> pendingRequests = messageRouterManager.RoutingDataManager.GetPendingRequests();

            if (pendingRequests.Count > 0)
            {
                IList<MessageRouterResult> messageRouterResults = new List<MessageRouterResult>();

                foreach (Party pendingRequest in pendingRequests)
                {
                    messageRouterResults.Add(messageRouterManager.RejectPendingRequest(pendingRequest));
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
