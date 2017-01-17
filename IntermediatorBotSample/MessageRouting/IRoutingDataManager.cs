using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;

namespace MessageRouting
{
    /// <summary>
    /// Interface for routing data managers.
    /// </summary>
    public interface IRoutingDataManager
    {
        #region Properties
        /// <summary>
        /// Parties that are users (not this bot).
        /// </summary>
        IList<Party> UserParties
        {
            get;
        }

        /// <summary>
        /// If the bot is addressed from different channels, its identity in terms of ID and name
        /// can vary. Those different identities are stored in this list.
        /// </summary>
        IList<Party> BotParties
        {
            get;
        }

        /// <summary>
        /// Represents the channel (and the specific conversation e.g. specific channel in Slack),
        /// where the chat requests are directed. For instance, this channel could be where the
        /// customer service agents accept customer chat requests.
        /// Edit: List of channels 
        /// </summary>
        IList<Party> AggregationParties
        {
            get;
        }

        /// <summary>
        /// The queue of parties waiting for their (conversation) requests to be accepted.
        /// </summary>
        List<Party> PendingRequests
        {
            get;
        }
        /// <summary>
        /// Contains 1:1 associations between parties i.e. parties engaged in a conversation.
        /// Furthermore, the key party is considered to be the conversation owner e.g. in
        /// a customer service situation the customer service agent.
        /// </summary>
        Dictionary<Party, Party> EngagedParties
        {
            get;
        }
        #endregion

        #region CRUD methods
        /// <summary>
        /// Adds the given party to the data.
        /// </summary>
        /// <param name="newParty">The new party to add.</param>
        /// <param name="isUser">If true, will try to add the party to the list of users.
        /// If false, will try to add it to the list of bot identities.</param>
        /// <returns>True, if the given party was added. False otherwise (was null or already stored).</returns>
        bool AddParty(Party newParty, bool isUser = true);

        /// <summary>
        /// Adds a new party with the given properties to the data.
        /// </summary>
        /// <param name="serviceUrl">The service URL.</param>
        /// <param name="channelId">The channel ID (e.g. "skype").</param>
        /// <param name="channelAccount">The channel account (read: user/bot ID).</param>
        /// <param name="conversationAccount">The conversation account (ID and name).</param>
        /// <param name="isUser">If true, will try to add the party to the list of users.
        /// If false, will try to add it to the list of bot identities.</param>
        /// <returns>True, if the party was added. False otherwise (identical party already stored).</returns>
        bool AddParty(string serviceUrl, string channelId,
            ChannelAccount channelAccount, ConversationAccount conversationAccount,
            bool isUser = true);

        /// <summary>
        /// Removes the party from all possible containers.
        /// Note that this method removes the party's all instances (user from all conversations).
        /// </summary>
        /// <param name="partyToRemove">The party to remove.</param>
        /// <returns>True, if the given party was removed. False otherwise.</returns>
        bool RemoveParty(Party partyToRemove);

        /// <summary>
        /// Deletes all existing routing data permanently.
        /// </summary>
        void DeleteAll();
        #endregion

        #region Utility methods
        /// <summary>
        /// Tries to find the existing user party (stored earlier) matching the given one.
        /// </summary>
        /// <param name="partyToFind">The party to find.</param>
        /// <returns>The existing party matching the given one.</returns>
        Party FindExistingUserParty(Party partyToFind);

        /// <summary>
        /// Tries to find a stored bot party instance matching the given channel ID and
        /// conversation account.
        /// </summary>
        /// <param name="channelId">The channel ID.</param>
        /// <param name="conversationAccount">The conversation account.</param>
        /// <returns>The bot party instance matching the given details or null if not found.</returns>
        Party FindBotPartyByChannelAndConversation(string channelId, ConversationAccount conversationAccount);

        /// <summary>
        /// Tries to find a stored party instance matching the given channel account ID and
        /// conversation ID.
        /// </summary>
        /// <param name="channelAccountId">The channel account ID (user ID).</param>
        /// <param name="conversationId">The conversation ID.</param>
        /// <returns>The party instance matching the given IDs or null if not found.</returns>
        Party FindPartyByChannelAccountIdAndConversationId(string channelAccountId, string conversationId);

        /// <summary>
        /// Tries to find a party engaged in a conversation.
        /// </summary>
        /// <param name="channelId">The channel ID.</param>
        /// <param name="channelAccount">The channel account.</param>
        /// <returns>The party matching the given details or null if not found.</returns>
        Party FindEngagedPartyByChannel(string channelId, ChannelAccount channelAccount);

        /// <summary>
        /// Finds the parties from the given list that match the channel account (and ID) of the given party.
        /// </summary>
        /// <param name="partyToFind">The party containing the channel details to match.</param>
        /// <param name="parties">The list of parties (candidates).</param>
        /// <returns>A newly created list of matching parties or null if none found.</returns>
        IList<Party> FindPartiesWithMatchingChannelAccount(Party partyToFind, IList<Party> parties);
        #endregion
    }
}
