using FizzWare.NBuilder;
using IntermediatorBotSample.Contracts;
using IntermediatorBotSample.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace IntermediatorBotSample.Controllers.Api
{
    [Route("api/[controller]")]    
    public class ConversationsController : Controller
    {
        private class DemoUserInformation : UserInformation
        {
            [JsonProperty(PropertyName = "isvip")]
            public bool IsVip { get; set; }

        }
        private readonly IExceptionHandler _exceptionHandler;


        public ConversationsController(IExceptionHandler exceptionHandler)
        {
            _exceptionHandler = exceptionHandler;
        }


        [HttpGet]
        public IEnumerable<Conversation> Get(int convId, int top)            
        {
            var channels = new[] { "facebook", "skype", "skype for business", "directline" };
            var random = new RandomGenerator();

            // 

            // return _exceptionHandler.GetAsync( () =>  _conversationManager.GetTopConversations(convId,top));

            return Builder<Conversation>.CreateListOfSize(5)
                .All()
                    .With(o =>  o.ConversationInformation   = Builder<ConversationInformation>.CreateNew()
                    .With(ci => ci.MessagesCount = random.Next(2, 30))
                    .With(ci => ci.SentimentScore = random.Next(0.0d, 1.0d))
                    .Build())
                    .With(o =>  o.ConversationReference     = Builder<ConversationReference>.CreateNew()
                    .With(cr => cr.ChannelId = channels[random.Next(0, channels.Count())])                    
                    .Build())
                    .With(o =>  o.UserInformation           = Builder<DemoUserInformation>.CreateNew()                    
                    .Build())
                .Build()
                .ToList();
        }


        //TODO: Retrieve ALL the conversation

        //TOOD: Forward conersation

        //TODO: DELETE Conversation = immediate kill by conversationId
    }
}