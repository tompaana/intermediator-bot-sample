using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace IntermediatorBotSample.ConversationHistory
{
    public class MessageLogEntity : TableEntity
    {
        public string Body
        {
            get;
            set;
        }
    }
}
