using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MessageRouting
{
    /// <summary>
    /// Routing data manager that stores the data locally.
    /// 
    /// NOTE: USE THIS CLASS ONLY FOR TESTING! Storing the data like this in production would
    /// not work since the bot may have multiple instances.
    /// 
    /// See IRoutingDataManager for general documentation of properties and methods.
    /// </summary>
    [Serializable]
    public class LocalRoutingDataManager : IRoutingDataManager
    {
        /// <summary>
        /// Parties that are users (not this bot).
        /// </summary>
        protected IList<Party> UserParties
        {
            get;
            set;
        }

        /// <summary>
        /// If the bot is addressed from different channels, its identity in terms of ID and name
        /// can vary. Those different identities are stored in this list.
        /// </summary>
        protected IList<Party> BotParties
        {
            get;
            set;
        }

        /// <summary>
        /// Represents the channels (and the specific conversations e.g. specific channel in Slack),
        /// where the chat requests are directed. For instance, a channel could be where the
        /// customer service agents accept customer chat requests. 
        /// </summary>
        protected IList<Party> AggregationParties
        {
            get;
            set;
        }

        /// <summary>
        /// The list of parties waiting for their (conversation) requests to be accepted.
        /// </summary>
        protected List<Party> PendingRequests
        {
            get;
            set;
        }

        /// <summary>
        /// Contains 1:1 associations between parties i.e. parties engaged in a conversation.
        /// Furthermore, the key party is considered to be the conversation owner e.g. in
        /// a customer service situation the customer service agent.
        /// </summary>
        protected Dictionary<Party, Party> EngagedParties
        {
            get;
            set;
        }

#if DEBUG
        protected IList<MessageRouterResult> LastMessageRouterResults
        {
            get;
            set;
        }
#endif

        /// <summary>
        /// Constructor.
        /// </summary>
        public LocalRoutingDataManager()
        {
            AggregationParties = new List<Party>();
            UserParties = new List<Party>();
            BotParties = new List<Party>();
            PendingRequests = new List<Party>();
            EngagedParties = new Dictionary<Party, Party>();
#if DEBUG
            LastMessageRouterResults = new List<MessageRouterResult>();
#endif
        }

        public virtual IList<Party> GetUserParties()
        {
            List<Party> userPartiesAsList = UserParties as List<Party>;
            return userPartiesAsList?.AsReadOnly();
        }

        public virtual IList<Party> GetBotParties()
        {
            List<Party> botPartiesAsList = BotParties as List<Party>;
            return botPartiesAsList?.AsReadOnly();
        }

        public virtual bool AddParty(Party newParty, bool isUser = true)
        {
            if (newParty == null || (isUser ? UserParties.Contains(newParty) : BotParties.Contains(newParty)))
            {
                return false;
            }

            if (isUser)
            {
                UserParties.Add(newParty);
            }
            else
            {
                if (newParty.ChannelAccount == null)
                {
                    throw new NullReferenceException($"Channel account of a bot party ({nameof(newParty.ChannelAccount)}) cannot be null");
                }

                BotParties.Add(newParty);
            }

            return true;
        }

        public virtual bool AddParty(string serviceUrl, string channelId,
            ChannelAccount channelAccount, ConversationAccount conversationAccount,
            bool isUser = true)
        {
            Party newParty = new Party(serviceUrl, channelId, channelAccount, conversationAccount);
            return AddParty(newParty, isUser);
        }

        public virtual IList<MessageRouterResult> RemoveParty(Party partyToRemove)
        {
            List<MessageRouterResult> messageRouterResults = new List<MessageRouterResult>();
            bool wasRemoved = false;

            // Check user and bot parties
            IList<Party>[] partyLists = new IList<Party>[]
            {
                UserParties,
                BotParties
            };

            foreach (IList<Party> partyList in partyLists)
            {
                IList<Party> partiesToRemove = FindPartiesWithMatchingChannelAccount(partyToRemove, partyList);

                if (partiesToRemove != null)
                {
                    foreach (Party party in partiesToRemove)
                    {
                        if (partiesToRemove.Remove(party))
                        {
                            wasRemoved = true;
                        }
                    }
                }
            }

            // Check pending requests
            IList<Party> pendingRequestsToRemove = FindPartiesWithMatchingChannelAccount(partyToRemove, PendingRequests);

            foreach (Party pendingRequestToRemove in pendingRequestsToRemove)
            {
                if (PendingRequests.Remove(pendingRequestToRemove))
                {
                    wasRemoved = true;

                    messageRouterResults.Add(new MessageRouterResult()
                    {
                        Type = MessageRouterResultType.EngagementRejected,
                        ConversationClientParty = pendingRequestToRemove
                    });
                }
            }

            if (wasRemoved)
            {
                // Check if the party exists in EngagedParties
                List<Party> keys = new List<Party>();

                foreach (var partyPair in EngagedParties)
                {
                    if (partyPair.Key.HasMatchingChannelInformation(partyToRemove)
                        || partyPair.Value.HasMatchingChannelInformation(partyToRemove))
                    {
                        keys.Add(partyPair.Key);
                    }
                }

                foreach (Party key in keys)
                {
                    messageRouterResults.AddRange(RemoveEngagement(key, EngagementProfile.Owner));
                }
            }

            return messageRouterResults;
        }

        public virtual IList<Party> GetAggregationParties()
        {
            List<Party> aggregationPartiesAsList = AggregationParties as List<Party>;
            return aggregationPartiesAsList?.AsReadOnly();
        }

        public virtual bool AddAggregationParty(Party party)
        {
            if (party != null)
            {
                if (party.ChannelAccount != null)
                {
                    throw new ArgumentException("Aggregation party cannot contain a channel account");
                }

                if (!AggregationParties.Contains(party))
                {
                    AggregationParties.Add(party);
                    return true;
                }
            }

            return false;
        }

        public virtual bool RemoveAggregationParty(Party party)
        {
            return AggregationParties.Remove(party);
        }

        public virtual IList<Party> GetPendingRequests()
        {
            List<Party> pendingRequestsAsList = PendingRequests as List<Party>;
            return pendingRequestsAsList?.AsReadOnly();
        }

        public virtual MessageRouterResult AddPendingRequest(Party party)
        {
            MessageRouterResult result = new MessageRouterResult()
            {
                ConversationClientParty = party
            };

            if (party != null)
            {
                if (PendingRequests.Contains(party))
                {
                    result.Type = MessageRouterResultType.EngagementAlreadyInitiated;
                }
                else
                {
                    PendingRequests.Add(party);
                    result.Type = MessageRouterResultType.EngagementInitiated;
                }
            }
            else
            {
                result.Type = MessageRouterResultType.Error;
                result.ErrorMessage = "The given party instance is null";
            }

            return result;
        }

        public virtual bool RemovePendingRequest(Party party)
        {
            return PendingRequests.Remove(party);
        }

        public virtual bool IsEngaged(Party party, EngagementProfile engagementProfile)
        {
            bool isEngaged = false;

            if (party != null)
            {
                switch (engagementProfile)
                {
                    case EngagementProfile.Client:
                        isEngaged = EngagedParties.Values.Contains(party);
                        break;
                    case EngagementProfile.Owner:
                        isEngaged = EngagedParties.Keys.Contains(party);
                        break;
                    case EngagementProfile.Any:
                        isEngaged = (EngagedParties.Values.Contains(party) || EngagedParties.Keys.Contains(party));
                        break;
                    default:
                        break;
                }
            }

            return isEngaged;
        }

        public virtual Party GetEngagedCounterpart(Party partyWhoseCounterpartToFind)
        {
            Party counterparty = null;

            if (IsEngaged(partyWhoseCounterpartToFind, EngagementProfile.Client))
            {
                for (int i = 0; i < EngagedParties.Count; ++i)
                {
                    if (EngagedParties.Values.ElementAt(i).Equals(partyWhoseCounterpartToFind))
                    {
                        counterparty = EngagedParties.Keys.ElementAt(i);
                        break;
                    }
                }
            }
            else if (IsEngaged(partyWhoseCounterpartToFind, EngagementProfile.Owner))
            {
                EngagedParties.TryGetValue(partyWhoseCounterpartToFind, out counterparty);
            }

            return counterparty;
        }

        public virtual MessageRouterResult AddEngagementAndClearPendingRequest(Party conversationOwnerParty, Party conversationClientParty)
        {
            MessageRouterResult result = new MessageRouterResult()
            {
                ConversationOwnerParty = conversationOwnerParty,
                ConversationClientParty = conversationClientParty
            };

            if (conversationOwnerParty != null && conversationClientParty != null)
            {
                try
                {
                    EngagedParties.Add(conversationOwnerParty, conversationClientParty);
                    PendingRequests.Remove(conversationClientParty);
                    result.Type = MessageRouterResultType.EngagementAdded;
                }
                catch (ArgumentException e)
                {
                    result.Type = MessageRouterResultType.Error;
                    result.ErrorMessage = e.Message;
                    System.Diagnostics.Debug.WriteLine($"Failed to add engagement between parties {conversationOwnerParty} and {conversationClientParty}: {e.Message}");
                }
            }
            else
            {
                result.Type = MessageRouterResultType.Error;
                result.ErrorMessage = "Either the owner or the client is missing";
            }

            return result;
        }

        public virtual IList<MessageRouterResult> RemoveEngagement(Party party, EngagementProfile engagementProfile)
        {
            IList<MessageRouterResult> messageRouterResults = new List<MessageRouterResult>();

            if (party != null)
            {
                List<Party> keysToRemove = new List<Party>();

                foreach (var partyPair in EngagedParties)
                {
                    bool removeThisPair = false;

                    switch (engagementProfile)
                    {
                        case EngagementProfile.Client:
                            removeThisPair = partyPair.Value.Equals(party);
                            break;
                        case EngagementProfile.Owner:
                            removeThisPair = partyPair.Key.Equals(party);
                            break;
                        case EngagementProfile.Any:
                            removeThisPair = (partyPair.Value.Equals(party) || partyPair.Key.Equals(party));
                            break;
                        default:
                            break;
                    }

                    if (removeThisPair)
                    {
                        keysToRemove.Add(partyPair.Key);

                        if (engagementProfile == EngagementProfile.Owner)
                        {
                            // Since owner is the key in the dictionary, there can be only one
                            break;
                        }
                    }
                }

                messageRouterResults = RemoveEngagements(keysToRemove);
            }

            return messageRouterResults;
        }

        public virtual void DeleteAll()
        {
            AggregationParties.Clear();
            UserParties.Clear();
            BotParties.Clear();
            PendingRequests.Clear();
            EngagedParties.Clear();
#if DEBUG
            LastMessageRouterResults.Clear();
#endif
        }

        public virtual bool IsAssociatedWithAggregation(Party party)
        {
            return (party != null && AggregationParties != null && AggregationParties.Count() > 0
                    && AggregationParties.Where(aggregationParty =>
                        aggregationParty.ConversationAccount.Id == party.ConversationAccount.Id
                        && aggregationParty.ServiceUrl == party.ServiceUrl
                        && aggregationParty.ChannelId == party.ChannelId).Count() > 0);
        }

        public virtual string ResolveBotNameInConversation(Party party)
        {
            string botName = null;

            if (party != null)
            {
                Party botParty = FindBotPartyByChannelAndConversation(party.ChannelId, party.ConversationAccount);

                if (botParty != null && botParty.ChannelAccount != null)
                {
                    botName = botParty.ChannelAccount.Name;
                }
            }

            return botName;
        }

        public virtual Party FindExistingUserParty(Party partyToFind)
        {
            Party foundParty = null;

            try
            {
                foundParty = UserParties.First(party => partyToFind.Equals(party));
            }
            catch (ArgumentNullException)
            {
            }
            catch (InvalidOperationException)
            {
            }

            return foundParty;
        }

        public virtual Party FindPartyByChannelAccountIdAndConversationId(string channelAccountId, string conversationId)
        {
            Party userParty = null;

            try
            {
                userParty = UserParties.Single(party =>
                        (party.ChannelAccount.Id.Equals(channelAccountId)
                         && party.ConversationAccount.Id.Equals(conversationId)));
            }
            catch (InvalidOperationException)
            {
            }

            return userParty;
        }

        public virtual Party FindBotPartyByChannelAndConversation(string channelId, ConversationAccount conversationAccount)
        {
            Party botParty = null;

            try
            {
                botParty = BotParties.Single(party =>
                        (party.ChannelId.Equals(channelId)
                         && party.ConversationAccount.Id.Equals(conversationAccount.Id)));
            }
            catch (InvalidOperationException)
            {
            }

            return botParty;
        }

        public virtual Party FindEngagedPartyByChannel(string channelId, ChannelAccount channelAccount)
        {
            Party foundParty = null;

            try
            {
                foundParty = EngagedParties.Keys.Single(party =>
                        (party.ChannelId.Equals(channelId)
                         && party.ChannelAccount != null
                         && party.ChannelAccount.Id.Equals(channelAccount.Id)));

                if (foundParty == null)
                {
                    // Not found in keys, try the values
                    foundParty = EngagedParties.Values.First(party =>
                            (party.ChannelId.Equals(channelId)
                             && party.ChannelAccount != null
                             && party.ChannelAccount.Id.Equals(channelAccount.Id)));
                }
            }
            catch (InvalidOperationException)
            {
            }

            return foundParty;
        }

        public virtual IList<Party> FindPartiesWithMatchingChannelAccount(Party partyToFind, IList<Party> parties)
        {
            IList<Party> matchingParties = null;
            IEnumerable<Party> foundParties = null;

            try
            {
                foundParties = UserParties.Where(party => party.HasMatchingChannelInformation(partyToFind));
            }
            catch (ArgumentNullException)
            {
            }
            catch (InvalidOperationException)
            {
            }

            if (foundParties != null)
            {
                matchingParties = foundParties.ToArray();
            }

            return matchingParties;
        }

        /// <returns>The engagements (parties in conversation) as a string.</returns>
        public string EngagementsAsString()
        {
            string parties = string.Empty;

            foreach (KeyValuePair<Party, Party> keyValuePair in EngagedParties)
            {
                parties += keyValuePair.Key + " -> " + keyValuePair.Value + "\n";
            }

            return parties;
        }

        /// <summary>
        /// Removes the engagements of the given conversation owners.
        /// </summary>
        /// <param name="conversationOwnerParties">The conversation owners whose engagements to remove.</param>
        /// <returns>The number of engagements removed.</returns>
        protected virtual IList<MessageRouterResult> RemoveEngagements(IList<Party> conversationOwnerParties)
        {
            IList<MessageRouterResult> messageRouterResults = new List<MessageRouterResult>();

            foreach (Party conversationOwnerParty in conversationOwnerParties)
            {
                Party conversationClientParty = null;
                EngagedParties.TryGetValue(conversationOwnerParty, out conversationClientParty);

                if (EngagedParties.Remove(conversationOwnerParty))
                {                   
                    messageRouterResults.Add(new MessageRouterResult()
                    {
                        Type = MessageRouterResultType.EngagementRemoved,
                        ConversationOwnerParty = conversationOwnerParty,
                        ConversationClientParty = conversationClientParty
                    });
                }
            }

            return messageRouterResults;
        }

#if DEBUG
        public string GetLastMessageRouterResults()
        {
            string lastResultsAsString = string.Empty;

            foreach (MessageRouterResult result in LastMessageRouterResults)
            {
                lastResultsAsString += $"{result.ToString()}\n";
            }

            return lastResultsAsString;
        }

        public void AddMessageRouterResult(MessageRouterResult result)
        {
            if (result != null)
            {
                if (LastMessageRouterResults.Count > 9)
                {
                    LastMessageRouterResults.Remove(LastMessageRouterResults.ElementAt(0));
                }

                LastMessageRouterResults.Add(result);
            }
        }
#endif
    }
}
