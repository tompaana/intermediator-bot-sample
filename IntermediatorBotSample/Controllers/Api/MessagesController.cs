using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FizzWare.NBuilder;
using IntermediatorBotSample.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IntermediatorBotSample.Controllers.Api
{
    [Produces("application/json")]
    [Route("api/luis")]
    public class MessagesController : Controller
    {        
        [HttpGet]
        public IEnumerable<Message> Get()
        {
            var random = new RandomGenerator();
            return Builder<Message>.CreateListOfSize(50)
                .All()
                .With(m => m.Text = random.Phrase(20))
                .Build()
                .ToList();
        }
    }
}