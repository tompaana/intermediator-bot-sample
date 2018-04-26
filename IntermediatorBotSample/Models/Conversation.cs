using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IntermediatorBotSample.Models
{
    [JsonObject(Title ="conversation")]
    public class Conversation
    {
        [JsonProperty(PropertyName = "userinformation")]
        public UserInformation UserInformation { get; set; }


        [JsonProperty(PropertyName = "conversationinformation")]
        public ConversationInformation ConversationInformation { get; set; }


        [JsonProperty(PropertyName = "conversationreference")]
        public ConversationReference ConversationReference { get; set; }
    }
}
