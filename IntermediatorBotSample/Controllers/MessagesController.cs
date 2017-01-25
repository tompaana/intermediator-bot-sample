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
            using (DefaultMessageRouterEventHandler messageRouterEventHandler = new DefaultMessageRouterEventHandler())
            {
                if (activity.Type == ActivityTypes.Message)
                {
                    // Get the message router manager instance
                    MessageRouterManager messageRouterManager = MessageRouterManager.Instance;

                    // Make sure we have the details of the sender and the receiver (bot) stored
                    messageRouterManager.MakeSurePartiesAreTracked(activity);

                    // Check for possible commands first
                    if (await messageRouterManager.BotCommandHandler.HandleBotCommandAsync(activity) == false)
                    {
                        // No command to the bot was issued so it must be a message then
                        if (await messageRouterManager.HandleMessageAsync(activity) == false)
                        {
                            // The message router manager failed to handle the message. This is likely
                            // due to the sender not being engaged in a conversation. Another reason
                            // could be that the manager has not been initialized.
                            //
                            // If you get here and you are sure that the manager has been initialized
                            // (note that initialization is only needed if there is an aggregation
                            // channel), you should either (depending on your use case):
                            //  1) Let the bot handle the message in the usual manner (e.g. let dialog
                            //     handle the message) or
                            //  2) automatically initiate the engagement, if the only thing this bot
                            //     does is forwards messages.

                            messageRouterManager.InitiateEngagement(activity);
                        }
                    }
                }
                else
                {
                    HandleSystemMessage(activity);
                }
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

                if (messageRouterManager.RoutingDataManager.RemoveParty(senderParty))
                {
                    return message.CreateReply($"Data of user {senderParty.ChannelAccount.Name} removed");
                }
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
                if (message.MembersRemoved != null && message.MembersRemoved.Count > 0)
                {
                    foreach (ChannelAccount channelAccount in message.MembersRemoved)
                    {
                        Party party = new Party(
                            message.ServiceUrl, message.ChannelId, channelAccount, message.Conversation);

                        if (messageRouterManager.RoutingDataManager.RemoveParty(party))
                        {
                            System.Diagnostics.Debug.WriteLine($"Party {party.ToString()} removed");
                        }
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
                // Handle knowing that the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }

        /*public new void Dispose()
        {
            base.Dispose();
            _messageRouterEventHander.Dispose();
        }*/
    }
}