using Newtonsoft.Json;
using System;

namespace IntermediatorBotSample.Models
{
    [JsonObject(Title ="conversationinformation")]
    public partial class ConversationInformation
    {
        [JsonProperty(PropertyName = "intialdate")]
        public DateTime InitialDate { get; set; }


        [JsonProperty(PropertyName = "sentimentscore")]
        public double SentimentScore { get; set; }


        [JsonProperty(PropertyName = "messagescount")]
        public int MessagesCount { get; set; }
    }
}
