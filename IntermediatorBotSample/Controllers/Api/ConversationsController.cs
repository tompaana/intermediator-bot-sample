using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FizzWare.NBuilder;
using IntermediatorBotSample.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace IntermediatorBotSample.Controllers.Api
{    
    [Route("api/[controller]")]    
    public class ConversationsController : Controller
    {
        [HttpGet]
        public IEnumerable<Conversation> Get()
        {
            return Builder<Conversation>.CreateListOfSize(2)
                .All()
                    .With(o =>  o.ConversationInformation   = Builder<ConversationInformation>.CreateNew().Build())
                    .With(o =>  o.ConversationReference     = Builder<ConversationReference>.CreateNew().Build())
                    .With(o =>  o.UserInformation           = Builder<UserInformation>.CreateNew().Build())
                .Build()
                .ToList();
        }
    }
}