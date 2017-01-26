using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MessageRouting
{
    /// <summary>
    /// Defines the type of engagement:
    /// - None: No engagement
    /// - Client: E.g. a customer
    /// - Owner: E.g. a customer service agent
    /// - Any: Either a client or an owner
    /// </summary>
    public enum EngagementProfile
    {
        None,
        Client,
        Owner,
        Any
    };

    /// <summary>
    /// Interface for routing data managers.
    /// </summary>
    public interface IRoutingDataManager
    {
        #region CRUD methods
        /// <returns>The user parties as a readonly list.</returns>
        IList<Party> GetUserParties();

        /// <returns>The bot parties as a readonly list.</returns>
        IList<Party> GetBotParties();

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
        /// <returns>A list of operation results.</returns>
        IList<MessageRouterResult> RemoveParty(Party partyToRemove);

        /// <returns>The aggregation parties as a readonly list.</returns>
        IList<Party> GetAggregationParties();

        /// <summary>
        /// Adds the given aggregation party.
        /// </summary>
        /// <param name="party">The party to be added as an aggregation party (channel).</param>
        /// <returns>True, if added. False otherwise (e.g. matching request already exists).</returns>
        bool AddAggregationParty(Party party);

        /// <summary>
        /// Removes the given aggregation party.
        /// </summary>
        /// <param name="party">The aggregation party to remove.</param>
        /// <returns>True, if removed successfully. False otherwise.</returns>
        bool RemoveAggregationParty(Party party);

        /// <returns>The (parties with) pending requests as a readonly list.</returns>
        IList<Party> GetPendingRequests();

        /// <summary>
        /// Adds the pending request for the given party.
        /// </summary>
        /// <param name="party">The party whose pending request to add.</param>
        /// <returns>The result of the operation.</returns>
        MessageRouterResult AddPendingRequest(Party party);

        /// <summary>
        /// Removes the pending request of the given party.
        /// </summary>
        /// <param name="party">The party whose request to remove.</param>
        /// <returns>True, if removed successfully. False otherwise.</returns>
        bool RemovePendingRequest(Party party);

        /// <summary>
        /// Checks if the given party is engaged in a 1:1 conversation as defined by the engagement
        /// profile (e.g. as a customer, as an agent or either one).
        /// </summary>
        /// <param name="party">The party to check.</param>
        /// <param name="engagementProfile">Defines whether to look for clients, owners or both.</param>
        /// <returns>True, if the party is engaged as defined by the given engagement profile.
        /// False otherwise.</returns>
        bool IsEngaged(Party party, EngagementProfile engagementProfile);

        /// <summary>
        /// Resolves the given party's counterpart in a 1:1 conversation.
        /// </summary>
        /// <param name="partyWhoseCounterpartToFind">The party whose counterpart to resolve.</param>
        /// <returns>The counterpart or null, if not found.</returns>
        Party GetEngagedCounterpart(Party partyWhoseCounterpartToFind);

        /// <summary>
        /// Creates a new engagement between the given parties. The method also clears the pending
        /// request of the client party, if one exists.
        /// </summary>
        /// <param name="conversationOwnerParty">The conversation owner party.</param>
        /// <param name="conversationClientParty">The conversation client (customer) party.</param>
        /// <returns>The result of the operation.</returns>
        MessageRouterResult AddEngagementAndClearPendingRequest(Party conversationOwnerParty, Party conversationClientParty);

        /// <summary>
        /// Removes an engagement(s) of the given party i.e. ends the 1:1 conversations.
        /// </summary>
        /// <param name="party">The party whose engagements to remove.</param>
        /// <param name="engagementProfile">The engagement profile of the party (owner/client/either).</param>
        /// <returns>A list of operation results.</returns>
        IList<MessageRouterResult> RemoveEngagement(Party party, EngagementProfile engagementProfile);

        /// <summary>
        /// Deletes all existing routing data permanently.
        /// </summary>
        void DeleteAll();
        #endregion

        #region Utility methods
        /// <summary>
        /// Checks if the given party is associated with aggregation. In human toung this means
        /// that the given party is, for instance, a customer service agent who deals with the
        /// requests coming from customers.
        /// </summary>
        /// <param name="party">The party to check.</param>
        /// <returns>True, if is associated. False otherwise.</returns>
        bool IsAssociatedWithAggregation(Party party);

        /// <summary>
        /// Tries to resolve the name of the bot in the same conversation with the given party.
        /// </summary>
        /// <param name="party">The party from whose perspective to resolve the name.</param>
        /// <returns>The name of the bot or null, if unable to resolve.</returns>
        string ResolveBotNameInConversation(Party party);

        /// <summary>
        /// Tries to find the existing user party (stored earlier) matching the given one.
        /// </summary>
        /// <param name="partyToFind">The party to find.</param>
        /// <returns>The existing party matching the given one.</returns>
        Party FindExistingUserParty(Party partyToFind);

        /// <summary>
        /// Tries to find a stored party instance matching the given channel account ID and
        /// conversation ID.
        /// </summary>
        /// <param name="channelAccountId">The channel account ID (user ID).</param>
        /// <param name="conversationId">The conversation ID.</param>
        /// <returns>The party instance matching the given IDs or null if not found.</returns>
        Party FindPartyByChannelAccountIdAndConversationId(string channelAccountId, string conversationId);

        /// <summary>
        /// Tries to find a stored bot party instance matching the given channel ID and
        /// conversation account.
        /// </summary>
        /// <param name="channelId">The channel ID.</param>
        /// <param name="conversationAccount">The conversation account.</param>
        /// <returns>The bot party instance matching the given details or null if not found.</returns>
        Party FindBotPartyByChannelAndConversation(string channelId, ConversationAccount conversationAccount);

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

        #region Methods for debugging
        /// <returns>The engagements (parties in conversation) as a string.
        /// Will return an empty string, if no engagements exist.</returns>
        string EngagementsAsString();
        #endregion
    }
}
