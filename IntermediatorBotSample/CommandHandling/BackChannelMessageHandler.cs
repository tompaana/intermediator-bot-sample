using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;
using System;
using Underscore.Bot.MessageRouting;
using Underscore.Bot.MessageRouting.DataStore;
using Underscore.Bot.Models;
using Underscore.Bot.Utils;

namespace IntermediatorBotSample.CommandHandling
{
    /// <summary>
    /// Handler for back channel messages.
    /// </summary>
    public class BackChannelMessageHandler
    {
        protected const string DefaultBackChannelId = "backchannel";
        protected const string DefaultPartyPropertyId = "conversationId";

        /// <summary>
        /// The routing data manager instance.
        /// </summary>
        public IRoutingDataManager RoutingDataManager
        {
            get;
            protected set;
        }

        /// <summary>
        /// The ID for back channel messages.
        /// </summary>
        public string BackChannelId
        {
            get;
            protected set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="backchannelId">The ID for back channel messages. If null, the default ID is used.</param>
        public BackChannelMessageHandler(IRoutingDataManager routingDataManager, string backchannelId = null)
        {
            RoutingDataManager = routingDataManager
                ?? throw new ArgumentNullException("Routing data manager instance must be given");

            if (string.IsNullOrEmpty(backchannelId))
            {
                BackChannelId = DefaultBackChannelId;
            }
            else
            {
                BackChannelId = backchannelId;
            }
        }

        /// <summary>
        /// Checks the given activity for back channel messages and handles them, if detected.
        /// Currently the only back channel message supported is for adding engagements
        /// (establishing 1:1 conversations).
        /// </summary>
        /// <param name="activity">The activity to check for back channel messages.</param>
        /// <returns>
        /// The result:
        ///     * MessageRouterResultType.EngagementAdded: An engagement (1:1 conversation) was created
        ///     * MessageRouterResultType.NoActionTaken: No back channel message detected
        ///     * MessageRouterResultType.Error: See the error message for details
        /// </returns>
        public MessageRouterResult HandleBackChannelMessage(Activity activity)
        {
            MessageRouterResult messageRouterResult = new MessageRouterResult();

            if (activity == null || string.IsNullOrEmpty(activity.Text))
            {
                messageRouterResult.Type = MessageRouterResultType.Error;
                messageRouterResult.ErrorMessage = $"The given activity ({nameof(activity)}) is either null or the message is missing";
            }
            else if (activity.Text.StartsWith(BackChannelId))
            {
                if (activity.ChannelData == null)
                {
                    messageRouterResult.Type = MessageRouterResultType.Error;
                    messageRouterResult.ErrorMessage = "No channel data";
                }
                else
                {
                    // Handle accepted request and start 1:1 conversation
                    string partyAsJsonString = ((JObject)activity.ChannelData)[BackChannelId][DefaultPartyPropertyId].ToString();
                    Party conversationClientParty = Party.FromJsonString(partyAsJsonString);

                    Party conversationOwnerParty = MessagingUtils.CreateSenderParty(activity);

                    messageRouterResult = RoutingDataManager.AddEngagementAndClearPendingRequest(
                        conversationOwnerParty, conversationClientParty);
                    messageRouterResult.Activity = activity;
                }
            }
            else
            {
                // No back channel message detected
                messageRouterResult.Type = MessageRouterResultType.NoActionTaken;
            }

            return messageRouterResult;
        }
    }
}