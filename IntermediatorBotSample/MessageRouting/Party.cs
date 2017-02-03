using Microsoft.Bot.Connector;
using System;

namespace MessageRouting
{
    /// <summary>
    /// Represents a party in a conversation, for example:
    /// - A specific user on a specific channel and in a specific conversation (e.g. channel in Slack)
    /// - Everyone (i.e. group) on a specific channel and in a specific conversation
    /// - Simply a specific channel and in a specific conversation (which is technically the same as the one above)
    /// 
    /// If the ChannelAccount property of this class is null, it means the two last cases mentioned.
    /// </summary>
    [Serializable]
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

        public string Language
        {
            get;
            set;
        }

        private const char PropertySeparator = ';';

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
            Language = string.Empty;
        }

        /// <summary>
        /// Checks if the channel ID and channel account ID match the ones of the given party.
        /// Note that this method works only on users/bots - not with channels (where ChannelAccount is null).
        /// </summary>
        /// <param name="other">Another party.</param>
        /// <returns>True, if the channel information is a match. False otherwise.</returns>
        public bool HasMatchingChannelInformation(Party other)
        {
            return (other != null
                && other.ChannelId.Equals(ChannelId)
                && other.ChannelAccount != null
                && ChannelAccount != null
                && other.ChannelAccount.Id.Equals(ChannelAccount.Id));
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

        public string ToIdString()
        {
            return ServiceUrl + PropertySeparator
                + ChannelId + PropertySeparator
                + ChannelAccount?.Id + PropertySeparator
                + ChannelAccount?.Name + PropertySeparator
                + ConversationAccount.Id + PropertySeparator
                + ConversationAccount.Name;
        }

        public static Party FromIdString(string idString)
        {
            Party party = null;

            if (!string.IsNullOrEmpty(idString))
            {
                string[] properties = idString.Split(PropertySeparator);

                if (properties.Length == 6)
                {
                    party = new Party(
                        properties[0], // Service URL
                        properties[1], // Channel ID
                        new ChannelAccount(id: properties[2], name: properties[3]),
                        new ConversationAccount(id: properties[4], name: properties[5]));
                }
            }

            return party;
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