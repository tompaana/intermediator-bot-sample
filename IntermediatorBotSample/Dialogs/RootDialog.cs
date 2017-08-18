using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;

namespace IntermediatorBotSample.Dialogs
{
    /// <summary>
    /// Simple echo dialog that initiates an engagement, if the message contains a certain keyword.
    /// </summary>
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        #pragma warning disable 1998
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(OnMessageReceivedAsync);
        }
        #pragma warning restore 1998

        /// <summary>
        /// Responds back to the sender with the message received or in case the message contains
        /// a specific keyword, will try to initiate an engagement (1:1 conversation).
        /// </summary>
        /// <param name="context"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task OnMessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            IMessageActivity messageActivity = await result;
            string message = messageActivity.Text;

            if (!string.IsNullOrEmpty(message))
            {
                if (message.ToLower().Contains("help"))
                {
                    WebApiConfig.MessageRouterManager.InitiateEngagement((messageActivity as Activity));
                }
                else
                {
                    messageActivity = context.MakeMessage();
                    messageActivity.Text = $"You said: " + message;
                    await context.PostAsync(messageActivity);
                }
            }

            context.Done(this);
        }
    }
}