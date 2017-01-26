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
                // Get the message router manager instance and let it handle the activity
                MessageRouterResult result = await MessageRouterManager.Instance.HandleActivityAsync(activity, true);

                if (result.Type == MessageRouterResultType.NoActionTaken)
                {
                    // No action was taken
                    // You can forward the activity to e.g. a dialog here
                }
            }
            else
            {
                await HandleSystemMessageAsync(activity);
            }

            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private async Task<Activity> HandleSystemMessageAsync(Activity message)
        {
            MessageRouterManager messageRouterManager = MessageRouterManager.Instance;

            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
                Party senderParty = MessagingUtils.CreateSenderParty(message);

                if (await messageRouterManager.RoutingDataManager.RemovePartyAsync(senderParty))
                {
                    return message.CreateReply($"Data of user {senderParty.ChannelAccount?.Name} removed");
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

                        if (await messageRouterManager.RoutingDataManager.RemovePartyAsync(party))
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