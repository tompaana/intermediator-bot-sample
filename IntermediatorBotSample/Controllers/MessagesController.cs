using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Connector;
using IntermediatorBotSample.Dialogs;
using Microsoft.Bot.Builder.Dialogs;
using Underscore.Bot.MessageRouting;
using Underscore.Bot.Utils;
using Underscore.Bot.Models;

namespace IntermediatorBotSample.Controllers
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
                MessageRouterManager messageRouterManager = WebApiConfig.MessageRouterManager;
                IMessageRouterResultHandler messageRouterResultHandler = WebApiConfig.MessageRouterResultHandler;

                // First check for commands
                if (await WebApiConfig.BotCommandHandler.HandleCommandAsync(
                        activity, messageRouterManager, messageRouterResultHandler) == false)
                {
                    // No command detected

                    // Get the message router manager instance and let it handle the activity
                    MessageRouterResult messageRouterResult = await messageRouterManager.HandleActivityAsync(activity, false);

                    if (messageRouterResult.Type == MessageRouterResultType.NoActionTaken)
                    {
                        // No action was taken by the message router manager. This means that the user
                        // is not engaged in a 1:1 conversation with a human (e.g. customer service
                        // agent) yet.
                        //
                        // You can, for example, check if the user (customer) needs human assistance
                        // here or forward the activity to a dialog. You could also do the check in
                        // the dialog too...
                        //
                        // Here's an example:
                        if (!string.IsNullOrEmpty(activity.Text) && activity.Text.ToLower().Contains("human"))
                        {
                            messageRouterResult = messageRouterManager.InitiateEngagement(activity);
                        }
                        else
                        {
                            await Conversation.SendAsync(activity, () => new RootDialog());
                        }
                    }

                    await messageRouterResultHandler.HandleResultAsync(messageRouterResult);
                }
            }
            else
            {
                await HandleSystemMessageAsync(activity);
            }

            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

#pragma warning disable 1998
        private async Task<Activity> HandleSystemMessageAsync(Activity message)
        {
            MessageRouterManager messageRouterManager = WebApiConfig.MessageRouterManager;

            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
                Party senderParty = MessagingUtils.CreateSenderParty(message);

                if (messageRouterManager.RemoveParty(senderParty)?.Count > 0)
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

                        if (messageRouterManager.RemoveParty(party)?.Count > 0)
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
#pragma warning restore 1998
    }
}