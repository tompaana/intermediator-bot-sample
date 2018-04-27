using IntermediatorBotSample.Contracts;
using IntermediatorBotSample.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace IntermediatorBotSample.Controllers.Api
{
    [Route("api/[controller]")]    
    public class ConversationsController : Controller
    {
        // Services        
        private readonly IConversationManager _conversationManager;


        public ConversationsController(IConversationManager conversationManager)
        {            
            _conversationManager = conversationManager;
        }


        [HttpPost("{channelId}/{conversationId}/{userId}/history")]
        public void Post(string channelId, string conversationId, string userId)
        {
            _conversationManager.TransmitMessageHistoryProactively(channelId, conversationId, userId);
        }


        [HttpDelete("{channelId}/{conversationId}")]
        public void Delete(string channelId, string conversationId)
        {
            _conversationManager.DeleteConversation(channelId, conversationId);
        }


        [HttpGet]
        public IEnumerable<Conversation> Get(int top = 10)            
        {
            return _conversationManager.GetConversations(top);
        }
    }
}