using Microsoft.AspNetCore.Mvc;
using System;

namespace IntermediatorBotSample.Controllers
{
    [Route("api/[controller]")]    
    public class BackchannelController : Controller
    {
        public BackchannelController()
        {            
        }

        [HttpPost("{channelId}/{conversationId}/{userId}/history")]
        public void Post(string channelId, string conversationId, string userId)
        {

        }

        [HttpDelete("{channelId}/{conversationId}")]
        public void Delete(string channelId, string conversationId)
        {

        }
    }
}
