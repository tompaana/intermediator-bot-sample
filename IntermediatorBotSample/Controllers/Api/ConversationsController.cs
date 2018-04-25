using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace IntermediatorBotSample.Controllers.Api
{    
    [Route("api/[controller]")]    
    public class ConversationsController : Controller
    {
        [HttpGet]
        public IEnumerable<string> Get()
        {
            var stupidResult = new
            {
                Name   = "Jorge",
                Value  = "Hello World",
                Reason = "Cuz I can!"
            };
            yield return JsonConvert.SerializeObject(stupidResult);
        }
    }
}