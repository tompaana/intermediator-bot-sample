using MessageRouting;
using System;
using System.Threading.Tasks;

namespace IntermediatorBot
{
    /// <summary>
    /// A simple result handler, which only echoes back the result, for debugging.
    /// </summary>
    public class MyDebugMessageRouterResultHandler : IMessageRouterResultHandler
    {
        /// <summary>
        /// From IMessageRouterResultHandler.
        /// </summary>
        /// <param name="messageRouterResult">The result to handle.</param>
        /// <returns></returns>
        public virtual async Task HandleResultAsync(MessageRouterResult messageRouterResult)
        {
            if (messageRouterResult == null)
            {
                throw new ArgumentNullException($"The given result ({nameof(messageRouterResult)}) is null");
            }

            string messageRouterResultAsString = messageRouterResult.ToString();

            if (messageRouterResult.Activity != null)
            {
                await MessagingUtils.ReplyToActivityAsync(messageRouterResult.Activity, messageRouterResultAsString);
            }
            else
            {
                MessageRouterManager messageRouterManager = MessageRouterManager.Instance;

                if (messageRouterResult.ConversationOwnerParty != null)
                {
                    await messageRouterManager.SendMessageToPartyByBotAsync(
                        messageRouterResult.ConversationOwnerParty, messageRouterResultAsString);
                }

                if (messageRouterResult.ConversationClientParty != null)
                {
                    await messageRouterManager.SendMessageToPartyByBotAsync(
                        messageRouterResult.ConversationClientParty, messageRouterResultAsString);
                }
            }
        }
    }
}