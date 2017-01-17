using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Bot.Connector;

namespace MessageRouting
{
    /// <summary>
    /// Routing data manager that stores the data in Azure Table storage services.
    /// </summary>
    public class AzureTableStorageRoutingDataManager : IRoutingDataManager
    {
        public IList<Party> UserParties => throw new NotImplementedException();

        public IList<Party> BotParties => throw new NotImplementedException();

        public IList<Party> AggregationParties => throw new NotImplementedException();

        public List<Party> PendingRequests => throw new NotImplementedException();

        public Dictionary<Party, Party> EngagedParties => throw new NotImplementedException();

        public bool AddParty(Party newParty, bool isUser = true)
        {
            throw new NotImplementedException();
        }

        public bool AddParty(string serviceUrl, string channelId,
            ChannelAccount channelAccount, ConversationAccount conversationAccount,
            bool isUser = true)
        {
            throw new NotImplementedException();
        }

        public bool RemoveParty(Party partyToRemove)
        {
            throw new NotImplementedException();
        }

        public void DeleteAll()
        {
            throw new NotImplementedException();
        }

        public Party FindBotPartyByChannelAndConversation(string channelId, ConversationAccount conversationAccount)
        {
            throw new NotImplementedException();
        }

        public Party FindEngagedPartyByChannel(string channelId, ChannelAccount channelAccount)
        {
            throw new NotImplementedException();
        }

        public Party FindExistingUserParty(Party partyToFind)
        {
            throw new NotImplementedException();
        }

        public IList<Party> FindPartiesWithMatchingChannelAccount(Party partyToFind, IList<Party> parties)
        {
            throw new NotImplementedException();
        }

        public Party FindPartyByChannelAccountIdAndConversationId(string channelAccountId, string conversationId)
        {
            throw new NotImplementedException();
        }
    }
}