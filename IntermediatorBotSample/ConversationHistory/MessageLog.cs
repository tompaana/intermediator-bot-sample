using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace IntermediatorBotSample.ConversationHistory
{
    public class MessageLog
    {
        /// <summary>
        /// The messages in the log.
        /// </summary>
        public IList<Activity> Activities
        {
            get;
            private set;
        }

        /// <summary>
        /// The user associated with this message log.
        /// </summary>
        public ConversationReference User
        {
            get;
            private set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="user">The user associated with this message log.</param>
        public MessageLog(ConversationReference user)
        {
            User = user;
            Activities = new List<Activity>();
        }

        /// <summary>
        /// Adds a message (an activity) to the log.
        /// </summary>
        /// <param name="activity">The activity to add.</param>
        public void AddMessage(Activity activity)
        {
            Activities.Add(activity);
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static MessageLog FromJson(string messageLogAsJsonString)
        {
            MessageLog messageLog = null;

            try
            {
                messageLog = JsonConvert.DeserializeObject<MessageLog>(messageLogAsJsonString);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to deserialize message log: {e.Message}");
            }

            return messageLog;
        }
    }
}
