using Microsoft.Bot.Schema;
using System;
using System.Runtime.CompilerServices;
using Underscore.Bot.MessageRouting;
using Underscore.Bot.MessageRouting.Logging;

namespace IntermediatorBotSample.Logging
{
    /// <summary>
    /// Logger that outputs all log messages to all aggregation channel conversations.
    /// </summary>
    public class AggregationChannelLogger : ILogger
    {
        private MessageRouter _messageRouter;

        public AggregationChannelLogger(MessageRouter messageRouter)
        {
            _messageRouter = messageRouter;
        }

        public async void Log(string message, [CallerMemberName] string methodName = "")
        {
            if (!string.IsNullOrWhiteSpace(methodName))
            {
                message = $"{DateTime.Now}> {methodName}: {message}";
            }

            bool wasSent = false;

            foreach (ConversationReference aggregationChannel in
                _messageRouter.RoutingDataManager.GetAggregationChannels())
            {
                ResourceResponse resourceResponse =
                    await _messageRouter.SendMessageAsync(aggregationChannel, message);

                if (resourceResponse != null)
                {
                    wasSent = true;
                }
            }

            if (!wasSent)
            {
                System.Diagnostics.Debug.WriteLine(message);
            }
        }
    }
}
