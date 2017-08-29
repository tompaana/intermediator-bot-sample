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
        public const string DefaultBackChannelId = "backchannel";
        public const string DefaultPartyKey = "conversationId";
        protected string EndOfLine = "\\r\\n";
        protected const char Backslash = '\\';
        protected const char QuotationMark = '"';

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
        /// The key identifying the serialized party data in the back channel message.
        /// </summary>
        public string PartyKey
        {
            get;
            protected set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="backchannelId">The ID for back channel messages. If null, the default value is used.</param>
        /// <param name="partyKey">The key identifying the serialized party data. If null, the default value is used.</param>
        public BackChannelMessageHandler(IRoutingDataManager routingDataManager, string backChannelId = null, string partyKey = null)
        {
            RoutingDataManager = routingDataManager
                ?? throw new ArgumentNullException("Routing data manager instance must be given");

            BackChannelId = string.IsNullOrEmpty(backChannelId) ? DefaultBackChannelId : backChannelId;
            PartyKey = string.IsNullOrEmpty(partyKey) ? DefaultPartyKey : partyKey;
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
            else if (activity.Text.Equals(BackChannelId))
            {
                if (activity.ChannelData == null)
                {
                    messageRouterResult.Type = MessageRouterResultType.Error;
                    messageRouterResult.ErrorMessage = "No channel data";
                }
                else
                {
                    // Handle accepted request and start 1:1 conversation
                    Party conversationClientParty = null;

                    try
                    {
                        conversationClientParty = ParsePartyFromChannelData(activity.ChannelData);
                    }
                    catch (Exception e)
                    {
                        messageRouterResult.Type = MessageRouterResultType.Error;
                        messageRouterResult.ErrorMessage =
                            $"Failed to parse the party information from the back channel message: {e.Message}";
                    }

                    if (conversationClientParty != null)
                    {
                        Party conversationOwnerParty = MessagingUtils.CreateSenderParty(activity);

                        messageRouterResult = RoutingDataManager.AddEngagementAndClearPendingRequest(
                            conversationOwnerParty, conversationClientParty);

                        messageRouterResult.Activity = activity;
                    }
                }
            }
            else
            {
                // No back channel message detected
                messageRouterResult.Type = MessageRouterResultType.NoActionTaken;
            }

            return messageRouterResult;
        }

        /// <summary>
        /// Tries to parse the party information and deserialize the party instance from the given
        /// channel data object.
        /// </summary>
        /// <param name="channelData">The channel data object to parse.</param>
        /// <returns>A deserialized party instance.</returns>
        protected Party ParsePartyFromChannelData(object channelData)
        {
            string partyAsJsonString = ((JObject)channelData)[BackChannelId][PartyKey].ToString();

            if (string.IsNullOrEmpty(partyAsJsonString))
            {
                throw new NullReferenceException("Failed to find the party information from the channel data");
            }

            partyAsJsonString = partyAsJsonString.Replace(EndOfLine, string.Empty);
            partyAsJsonString = partyAsJsonString.Replace(Backslash, QuotationMark);
            return Party.FromJsonString(partyAsJsonString);
        }
    }
}