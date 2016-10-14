using Microsoft.Bot.Connector;
using System;

namespace IntermediatorBotSample
{
    /// <summary>
    /// Represents a party in a conversation, for example:
    /// - A specific user on a specific channel and in a specific conversation (e.g. channel in Slack)
    /// - Everyone (i.e. group) on a specific channel and in a specific conversation
    /// - Simply a specific channel and in a specific conversation (which is technically the same as the one above)
    /// 
    /// If the ChannelAccount property of this class is null, it means the two last cases mentioned.
    /// </summary>
    public class Party : IEquatable<Party>
    {
        public string ServiceUrl
        {
            get;
            private set;
        }

        public string ChannelId
        {
            get;
            private set;
        }

        /// <summary>
        /// Conversation account - represents a specific user.
        /// 
        /// Can be null and if so this party is considered to cover (everyone in the given)
        /// channel/conversation.
        /// </summary>
        public ChannelAccount ChannelAccount
        {
            get;
            private set;
        }

        public ConversationAccount ConversationAccount
        {
            get;
            private set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="serviceUrl">Service URL. Must be provided.</param>
        /// <param name="channelId">Channel ID. Must be provided.</param>
        /// <param name="channelAccount">Channel account (represents a specific user). Can be null
        /// and if so this party is considered to cover everyone in the given channel.
        /// <param name="conversationAccount">Conversation account. Must contain a valid ID.</param>
        public Party(string serviceUrl, string channelId,
            ChannelAccount channelAccount, ConversationAccount conversationAccount)
        {
            if (string.IsNullOrEmpty(serviceUrl))
            {
                throw new AccessViolationException(nameof(serviceUrl));
            }

            if (string.IsNullOrEmpty(channelId))
            {
                throw new ArgumentException(nameof(channelId));
            }

            if (conversationAccount == null || string.IsNullOrEmpty(conversationAccount.Id))
            {
                throw new ArgumentException(nameof(conversationAccount));
            }

            ServiceUrl = serviceUrl;
            ChannelId = channelId;
            ChannelAccount = channelAccount;
            ConversationAccount = conversationAccount;
        }

        /// <summary>
        /// Checks if the given party is part of the same conversation as this one.
        /// I.e. the service URL, channel ID and conversation details need to match.
        /// </summary>
        /// <param name="other">Another party.</param>
        /// <returns>True, if these are part of same conversation. False otherwise.</returns>
        public bool IsPartOfSameConversation(Party other)
        {
            return (other != null
                && other.ServiceUrl.Equals(ServiceUrl)
                && other.ChannelId.Equals(ChannelId)
                && other.ConversationAccount.Id.Equals(ConversationAccount.Id));
        }

        public bool Equals(Party other)
        {
            return (IsPartOfSameConversation(other)
                && ((other.ChannelAccount == null && ChannelAccount == null)
                    || (other.ChannelAccount != null && ChannelAccount != null
                        && other.ChannelAccount.Id == ChannelAccount.Id)));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Party);
        }

        public override int GetHashCode()
        {
            string channelAccountId = (ChannelAccount != null) ? ChannelAccount.Id : "0";
            return new { ServiceUrl, ChannelId, channelAccountId, ConversationAccount.Id }.GetHashCode();
        }

        public override string ToString()
        {
            return "[" + ServiceUrl + "; " + ChannelId + "; "
                + ((ChannelAccount == null) ? "(no specific user); " : ("{" + ChannelAccount.Id + "; " + ChannelAccount.Name + "}; "))
                + "{" + ConversationAccount.Id + "; " + ConversationAccount.Name + "}]";
        }
    }
}