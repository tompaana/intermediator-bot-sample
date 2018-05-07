using IntermediatorBotSample.CommandHandling;
using IntermediatorBotSample.MessageRouting;
using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System;
using System.Threading.Tasks;
using Underscore.Bot.MessageRouting;

namespace IntermediatorBotSample
{
    public class IntermediatorBot : IBot
    {
        public async Task OnTurn(ITurnContext context)
        {
            Activity activity = context.Activity;
            

            if (activity.Type is ActivityTypes.Message)
            {
                MessageRouter messageRouter = Startup.HandoffHelper.MessageRouter;
                MessageRouterResultHandler messageRouterResultHandler = Startup.HandoffHelper.MessageRouterResultHandler;
                bool rejectConnectionRequestIfNoAggregationChannel    = Startup.BotSettings.RejectConnectionRequestIfNoAggregationChannel;

                messageRouter.StoreConversationReferences(activity);

                // First check for commands (both from back channel and the ones directly typed)
                MessageRouterResult messageRouterResult = Startup.HandoffHelper.BackChannelMessageHandler.HandleBackChannelMessage(activity);

                if (messageRouterResult.Type != MessageRouterResultType.Connected && await Startup.HandoffHelper.CommandMessageHandler.HandleCommandAsync(activity) == false)
                {
                    // No valid back channel (command) message or typed command detected

                    // Let the message router manager instance handle the activity
                    messageRouterResult = await messageRouter.HandleActivityAsync(
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
                        if (!string.IsNullOrEmpty(activity.Text) && activity.Text.ToLower().Contains(Commands.CommandRequestConnection))
                        {
                            messageRouterResult = messageRouter.CreateConnectionRequest(
                                MessageRouter.CreateSenderConversationReference(activity), rejectConnectionRequestIfNoAggregationChannel);
                        }
                        else
                        {
                            await context.SendActivity($"Hello world.");
                        }
                    }
                }

                // Uncomment to see the result in a reply (may be useful for debugging)
                //await MessagingUtils.ReplyToActivityAsync(activity, messageRouterResult.ToString());

                // Handle the result, if required
                await messageRouterResultHandler.HandleResultAsync(messageRouterResult);
            }
        }
    }
}
