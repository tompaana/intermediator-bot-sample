using IntermediatorBot.Strings;
using IntermediatorBotSample.CommandHandling;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Threading.Tasks;
using Underscore.Bot.MessageRouting;

namespace IntermediatorBotSample.Dialogs
{
    /// <summary>
    /// Simple echo dialog that tries to connect with a human, if the message contains the specific command.
    /// </summary>
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext dialogContext)
        {
            dialogContext.Wait(OnMessageReceivedAsync);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Responds back to the sender with the instructions or in case the message contains
        /// the specific command, will try to connect with a human (in 1:1 conversation).
        /// </summary>
        /// <param name="dialogContext">The dialog context.</param>
        /// <param name="result">The result containing the message sent by the user.</param>
        private async Task OnMessageReceivedAsync(IDialogContext dialogContext, IAwaitable<IMessageActivity> result)
        {
            IMessageActivity messageActivity = await result;
            string messageText = messageActivity.Text;

            if (!string.IsNullOrEmpty(messageText))
            {
                if (messageText.ToLower().Contains(Commands.CommandRequestConnection))
                {
                    MessageRouterResult messageRouterResult =
                        WebApiConfig.MessageRouterManager.RequestConnection(
                            (messageActivity as Activity), WebApiConfig.Settings.RejectConnectionRequestIfNoAggregationChannel);
                    await WebApiConfig.MessageRouterResultHandler.HandleResultAsync(messageRouterResult);
                }
                else
                {
                    messageActivity = dialogContext.MakeMessage();

                    messageActivity.Text =
                        $"* {string.Format(ConversationText.OptionsCommandHint, $"{Commands.CommandKeyword} {Commands.CommandListOptions}")}"
                        + $"\n\r* {string.Format(ConversationText.ConnectRequestCommandHint, Commands.CommandRequestConnection)}";

                    await dialogContext.PostAsync(messageActivity);
                }
            }

            dialogContext.Wait(OnMessageReceivedAsync);
        }
    }
}
