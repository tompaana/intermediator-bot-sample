using Microsoft.Bot.Connector;
using System;

namespace MessageRouting
{
    /// <summary>
    /// Utility methods.
    /// </summary>
    public class MessagingUtils
    {
        public struct ConnectorClientAndMessageBundle
        {
            public ConnectorClient connectorClient;
            public IMessageActivity messageActivity;
        }

        /// <summary>
        /// Constructs a party instance using the sender (from) of the given activity.
        /// </summary>
        /// <param name="activity"></param>
        /// <returns>A newly created Party instance.</returns>
        public static Party CreateSenderParty(IActivity activity)
        {
            return new Party(activity.ServiceUrl, activity.ChannelId, activity.From, activity.Conversation);
        }

        /// <summary>
        /// Constructs a party instance using the recipient of the given activity.
        /// </summary>
        /// <param name="activity"></param>
        /// <returns>A newly created Party instance.</returns>
        public static Party CreateRecipientParty(IActivity activity)
        {
            return new Party(activity.ServiceUrl, activity.ChannelId, activity.Recipient, activity.Conversation);
        }

        /// <summary>
        /// Creates a connector client with the given message activity for the given party as the
        /// recipient. If this party has an ID of a specific user (ChannelAccount is valid), then
        /// the that user is set as the recipient. Otherwise, the whole channel is addressed.
        /// </summary>
        /// <param name="serviceUrl">The service URL of the channel of the party to send the message to.</param>
        /// <param name="newMessageActivity">The message activity to send.</param>
        /// <returns>A bundle containing a newly created connector client (that is used to send
        /// the message and the message activity (the content of the message).</returns>
        public static ConnectorClientAndMessageBundle CreateConnectorClientAndMessageActivity(
            string serviceUrl, IMessageActivity newMessageActivity)
        {
            ConnectorClient newConnectorClient = new ConnectorClient(new Uri(serviceUrl));

            ConnectorClientAndMessageBundle bundle = new ConnectorClientAndMessageBundle()
            {
                connectorClient = newConnectorClient,
                messageActivity = newMessageActivity
            };

            return bundle;
        }

        /// <summary>
        /// For convenience.
        /// </summary>
        /// <param name="partyToMessage">The party to send the message to.</param>
        /// <param name="messageText">The message text content.</param>
        /// <param name="senderAccount">The channel account of the sender.</param>
        /// <returns>A bundle containing a newly created connector client (that is used to send
        /// the message and the message activity (the content of the message).</returns>
        public static ConnectorClientAndMessageBundle CreateConnectorClientAndMessageActivity(
            Party partyToMessage, string messageText, ChannelAccount senderAccount)
        {
            IMessageActivity newMessageActivity = Activity.CreateMessageActivity();
            newMessageActivity.Conversation = partyToMessage.ConversationAccount;
            newMessageActivity.Text = messageText;

            if (senderAccount != null)
            {
                newMessageActivity.From = senderAccount;
            }

            if (partyToMessage.ChannelAccount != null)
            {
                newMessageActivity.Recipient = partyToMessage.ChannelAccount;
            }

            return CreateConnectorClientAndMessageActivity(partyToMessage.ServiceUrl, newMessageActivity);
        }

        /// <summary>
        /// Strips the mentions from the message text.
        /// </summary>
        /// <param name="messageActivity"></param>
        /// <returns>The stripped message.</returns>
        public static string StripMentionsFromMessage(IMessageActivity messageActivity)
        {
            string strippedMessage = messageActivity.Text;

            if (!string.IsNullOrEmpty(strippedMessage))
            {
                Mention[] mentions = messageActivity.GetMentions();

                foreach (Mention mention in mentions)
                {
                    string mentionText = mention.Text;

                    if (!string.IsNullOrEmpty(mentionText))
                    {
                        while (strippedMessage.Contains(mentionText))
                        {
                            strippedMessage = strippedMessage.Remove(
                                strippedMessage.IndexOf(mentionText), mentionText.Length);
                        }

                    }
                }

                strippedMessage = strippedMessage.Trim();
            }

            return strippedMessage;
        }

        /// <summary>
        /// Checks whether the resource response was OK or not.
        /// </summary>
        /// <param name="resourceResponse">The resource response to check.</param>
        /// <returns>True, if the response was OK (e.g. message went through). False otherwise.</returns>
        public static bool WasSuccessful(ResourceResponse resourceResponse)
        {
            // TODO Check the ID too once we've fixed the gateway error that occurs in message relaying
            return (resourceResponse != null /*&& resourceResponse.Id != null*/);
        }
    }
}