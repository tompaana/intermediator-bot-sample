using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Bot.Connector;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Azure;

namespace MessageRouting
{
    /// <summary>
    /// Routing data manager that stores the data in Azure Table storage services.
    /// Caching policy: If the local query finds nothing, update the data from the storage.
    /// See IRoutingDataManager for general documentation of properties and methods.
    /// 
    /// NOTE: DO NOT USE THIS CLASS - THIS IS NOT FAR FROM A PLACEHOLDER CURRENTLY
    /// 
    /// See also Get started with Azure Table storage using .NET article:
    /// https://docs.microsoft.com/en-us/azure/storage/storage-dotnet-how-to-use-tables
    /// </summary>
    public class AzureTableStorageRoutingDataManager : IRoutingDataManager
    {
        private const string StorageConnectionStringId = "RoutingDataStorageConnectionString";
        private string _connectionString;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AzureTableStorageRoutingDataManager()
        {
            _connectionString = CloudConfigurationManager.GetSetting(StorageConnectionStringId);
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(_connectionString);
        }

        public bool AddAggregationParty(Party party)
        {
            throw new NotImplementedException();
        }

        public MessageRouterResult AddEngagementAndClearPendingRequest(Party conversationOwnerParty, Party conversationClientParty)
        {
            throw new NotImplementedException();
        }

        public bool AddParty(Party newParty, bool isUser = true)
        {
            throw new NotImplementedException();
        }

        public bool AddParty(string serviceUrl, string channelId, ChannelAccount channelAccount, ConversationAccount conversationAccount, bool isUser = true)
        {
            throw new NotImplementedException();
        }

        public MessageRouterResult AddPendingRequest(Party party)
        {
            throw new NotImplementedException();
        }

        public void DeleteAll()
        {
            throw new NotImplementedException();
        }

        public string EngagementsAsString()
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

        public IList<Party> GetAggregationParties()
        {
            throw new NotImplementedException();
        }

        public IList<Party> GetBotParties()
        {
            throw new NotImplementedException();
        }

        public Party GetEngagedCounterpart(Party partyWhoseCounterpartToFind)
        {
            throw new NotImplementedException();
        }

        public IList<Party> GetPendingRequests()
        {
            throw new NotImplementedException();
        }

        public IList<Party> GetUserParties()
        {
            throw new NotImplementedException();
        }

        public bool IsAssociatedWithAggregation(Party party)
        {
            throw new NotImplementedException();
        }

        public bool IsEngaged(Party party, EngagementProfile engagementProfile)
        {
            throw new NotImplementedException();
        }

        public bool RemoveAggregationParty(Party party)
        {
            throw new NotImplementedException();
        }

        public IList<MessageRouterResult> RemoveEngagement(Party party, EngagementProfile engagementProfile)
        {
            throw new NotImplementedException();
        }

        public IList<MessageRouterResult> RemoveParty(Party partyToRemove)
        {
            throw new NotImplementedException();
        }

        public bool RemovePendingRequest(Party party)
        {
            throw new NotImplementedException();
        }

        public string ResolveBotNameInConversation(Party party)
        {
            throw new NotImplementedException();
        }
    }
}