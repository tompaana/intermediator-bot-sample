using IntermediatorBot.Strings;
using IntermediatorBotSample.CommandHandling;
using IntermediatorBotSample.Dialogs;
using IntermediatorBotSample.MessageRouting;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
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
        }

        /// <summary>
        /// Handles the received message.
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Locale != null)
            {
                ConversationText.Culture = new CultureInfo(activity.Locale);
            }

            if (activity.Type == ActivityTypes.Message)
            {
                MessageRouterManager messageRouterManager = WebApiConfig.MessageRouterManager;
                MessageRouterResultHandler messageRouterResultHandler = WebApiConfig.MessageRouterResultHandler;
                bool rejectConnectionRequestIfNoAggregationChannel =
                    WebApiConfig.Settings.RejectConnectionRequestIfNoAggregationChannel;

                messageRouterManager.MakeSurePartiesAreTracked(activity);
                
                // First check for commands (both from back channel and the ones directly typed)
                MessageRouterResult messageRouterResult =
                    WebApiConfig.BackChannelMessageHandler.HandleBackChannelMessage(activity);

                if (messageRouterResult.Type != MessageRouterResultType.Connected
                    && await WebApiConfig.CommandMessageHandler.HandleCommandAsync(activity) == false)
                {
                    // No valid back channel (command) message or typed command detected

                    // Let the message router manager instance handle the activity
                    messageRouterResult = await messageRouterManager.HandleActivityAsync(
                        activity, false, rejectConnectionRequestIfNoAggregationChannel);

                    if (messageRouterResult.Type == MessageRouterResultType.NoActionTaken)
                    {
                        // No action was taken by the message router manager. This means that the
                        // user is not connected (in a 1:1 conversation) with a human
                        // (e.g. customer service agent) yet.
                        //
                        // You can, for example, check if the user (customer) needs human
                        // assistance here or forward the activity to a dialog. You could also do
                        // the check in the dialog too...
                        //
                        // Here's an example:
                        if (!string.IsNullOrEmpty(activity.Text)
                            && activity.Text.ToLower().Contains(Commands.CommandRequestConnection))
                        {
                            messageRouterResult = messageRouterManager.RequestConnection(
                                activity, rejectConnectionRequestIfNoAggregationChannel);
                        }
                        else
                        {
                            await Conversation.SendAsync(activity, () => new RootDialog());
                        }
                    }
                }

                // Uncomment to see the result in a reply (may be useful for debugging)
                //await MessagingUtils.ReplyToActivityAsync(activity, messageRouterResult.ToString());

                // Handle the result, if required
                await messageRouterResultHandler.HandleResultAsync(messageRouterResult);
            }
            else
            {
                HandleSystemMessage(activity);
            }

            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private void HandleSystemMessage(Activity activity)
        {
            MessageRouterManager messageRouterManager = WebApiConfig.MessageRouterManager;

            if (activity.Type == ActivityTypes.DeleteUserData)
            {
                Party senderParty = MessagingUtils.CreateSenderParty(activity);
                IList<MessageRouterResult> messageRouterResults = messageRouterManager.RemoveParty(senderParty);

                foreach (MessageRouterResult messageRouterResult in messageRouterResults)
                {
                    if (messageRouterResult.Type == MessageRouterResultType.OK)
                    {
                        System.Diagnostics.Debug.WriteLine(ConversationText.UserDataDeleted, senderParty.ChannelAccount?.Name);
                    }
                }
            }
            else if (activity.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
                if (activity.MembersRemoved != null && activity.MembersRemoved.Count > 0)
                {
                    foreach (ChannelAccount channelAccount in activity.MembersRemoved)
                    {
                        Party partyToRemove = new Party(activity.ServiceUrl, activity.ChannelId, channelAccount, activity.Conversation);
                        IList<MessageRouterResult> messageRouterResults = messageRouterManager.RemoveParty(partyToRemove);

                        foreach (MessageRouterResult messageRouterResult in messageRouterResults)
                        {
                            if (messageRouterResult.Type == MessageRouterResultType.OK)
                            {
                                System.Diagnostics.Debug.WriteLine(ConversationText.PartyRemoved, partyToRemove.ChannelAccount?.Name);
                            }
                        }
                    }
                }
            }
            else if (activity.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (activity.Type == ActivityTypes.Typing)
            {
                // Handle knowing that the user is typing
            }
            else if (activity.Type == ActivityTypes.Ping)
            {
            }
        }
    }
}