using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IntermediatorBotSample.Models
{
    [JsonObject(Title ="conversationinformation")]
    public class ConversationInformation
    {
        [JsonProperty(PropertyName = "intialdate")]
        public DateTime InitialDate { get; set; }


        [JsonProperty(PropertyName = "sentimentscore")]
        public double SentimentScore { get; set; }


        [JsonProperty(PropertyName = "messagescount")]
        public int MessagesCount { get; set; }
    }
}
