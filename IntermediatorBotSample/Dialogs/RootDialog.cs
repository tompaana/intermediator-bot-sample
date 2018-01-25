using IntermediatorBot.Strings;
using IntermediatorBotSample.CommandHandling;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Threading.Tasks;

namespace IntermediatorBotSample.Dialogs
{
    /// <summary>
    /// Simple dialog that will only ever provide simple instructions.
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
        /// Responds back to the sender with the simple instructions.
        /// </summary>
        /// <param name="dialogContext">The dialog context.</param>
        /// <param name="result">The result containing the message sent by the user.</param>
        private async Task OnMessageReceivedAsync(IDialogContext dialogContext, IAwaitable<IMessageActivity> result)
        {
            IMessageActivity messageActivity = await result;
            string messageText = messageActivity.Text;

            if (!string.IsNullOrEmpty(messageText))
            {
                messageActivity = dialogContext.MakeMessage();

                messageActivity.Text =
                    $"* {string.Format(ConversationText.OptionsCommandHint, $"{Commands.CommandKeyword} {Commands.CommandListOptions}")}"
                    + $"\n\r* {string.Format(ConversationText.ConnectRequestCommandHint, Commands.CommandRequestConnection)}";

                await dialogContext.PostAsync(messageActivity);
            }

            dialogContext.Wait(OnMessageReceivedAsync);
        }
    }
}
