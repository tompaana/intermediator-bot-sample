using IntermediatorBotSample.Models;
using System.Collections.Generic;

namespace IntermediatorBotSample.Contracts
{
    public interface IConversationManager
    {
        IEnumerable<Conversation> GetConversations(int top);

        void DeleteConversation(string channelId, string conversationId);
    }
}
