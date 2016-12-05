using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Connector;
using MessageRouting;

namespace IntermediatorBotSample
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        public MessagesController()
        {
            // Note: This class is constructed every time there is a new activity (Post called).
        }

        /// <summary>
        /// Handles the received message.
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                // Get the message router manager instance
                MessageRouterManager messageRouterManager = MessageRouterManager.Instance;

                // Make we have the details of the sender and the receiver (bot) stored
                messageRouterManager.MakeSurePartiesAreTracked(activity);

                // Check for possible commands first
                if (await messageRouterManager.HandleDirectCommandToBotAsync(activity) == false)
                {
                    // No command to the bot was issued so it must be a message then
                    messageRouterManager.HandleMessageAsync(activity);
                }
            }
            else
            {
                HandleSystemMessage(activity);
            }

            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            MessageRouterManager messageRouterManager = MessageRouterManager.Instance;

            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
                Party senderParty = MessagingUtils.CreateSenderParty(message);

                if (messageRouterManager.RemoveParty(senderParty))
                {
                    return message.CreateReply($"Data of user {senderParty.ChannelAccount.Name} removed");
                }
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
                foreach (ChannelAccount channelAccount in message.MembersRemoved)
                {
                    Party party = new Party(
                        message.ServiceUrl, message.ChannelId, channelAccount, message.Conversation);

                    if (messageRouterManager.RemoveParty(party))
                    {
                        System.Diagnostics.Debug.WriteLine($"Party {party.ToString()} removed");
                    }
                }
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}