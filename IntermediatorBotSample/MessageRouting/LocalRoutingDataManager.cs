using System;
using System.Collections.Generic;

namespace MessageRouting
{
    /// <summary>
    /// Container of data required for routing message.
    /// </summary>
    [Serializable]
    public class RoutingData
    {
        public IList<Party> UserParties
        {
            get;
            private set;
        }

        /// <summary>
        /// If the bot is addressed from different channels, its identity in terms of ID and name
        /// can vary. Those different identities are stored in this list.
        /// </summary>
        public IList<Party> BotParties
        {
            get;
            private set;
        }

        /// <summary>
        /// Contains 1:1 associations between parties i.e. parties engaged in a conversation.
        /// Furthermore, the key party is considered to be the conversation owner e.g. in
        /// a customer service situation the customer service agent.
        /// </summary>
        public Dictionary<Party, Party> EngagedParties
        {
            get;
            private set;
        }

        /// <summary>
        /// The queue of parties waiting for their (conversation) requests to be accepted.
        /// </summary>
        public List<Party> PendingRequests
        {
            get;
            private set;
        }

        /// <summary>
        /// Represents the channel (and the specific conversation e.g. specific channel in Slack),
        /// where the chat requests are directed. For instance, this channel could be where the
        /// customer service agents accept customer chat requests.
        /// Edit: List of channels 
        /// </summary>
        public IList<Party> AggregationParties
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public RoutingData()
        {
            AggregationParties = new List<Party>();
            UserParties = new List<Party>();
            BotParties = new List<Party>();
            PendingRequests = new List<Party>();
            EngagedParties = new Dictionary<Party, Party>();
        }

        /// <summary>
        /// Clears all data.
        /// </summary>
        public void Clear()
        {
            AggregationParties.Clear();
            UserParties.Clear();
            BotParties.Clear();
            PendingRequests.Clear();
            EngagedParties.Clear();
        }
    }
}