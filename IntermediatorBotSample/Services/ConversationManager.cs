using FizzWare.NBuilder;
using IntermediatorBotSample.Contracts;
using IntermediatorBotSample.Models;
using IntermediatorBotSample.Settings;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Underscore.Bot.MessageRouting.DataStore;
using CH = IntermediatorBotSample.ConversationHistory;

namespace IntermediatorBotSample.Services
{
    public class ConversationManager : IConversationManager
    {
        private readonly IExceptionHandler      _exceptionHandler;
        private readonly HandoffHelper          _handoffHelper;
        private readonly CH.ConversationHistory _conversationHistory;
        private readonly RoutingDataManager     _routingDataManager;

        public ConversationManager(IExceptionHandler exceptionHandler, RoutingDataManager routingDataManager, BotSettings botSettings, CH.ConversationHistory conversationHistory)
        {
            _exceptionHandler    = exceptionHandler;
            _handoffHelper       = new HandoffHelper(botSettings);
            _conversationHistory = conversationHistory;
            _routingDataManager  = routingDataManager;
        }


        public void DeleteConversation(string channelId, string conversationId)
        {
            var conversationReference = 
                _exceptionHandler.Get(() => 
                    _routingDataManager.FindConversationReference(channelId, conversationId)
                );

            if (conversationReference != null)
            {
                _exceptionHandler.Execute(() => _handoffHelper.MessageRouter.Disconnect(conversationReference));
            }
        }


        public IEnumerable<Conversation> GetConversations(int top)
        {
            if (top <= 0 || top > 50)
                return null;

            var channels           = new[] { "facebook", "skype", "skype for business", "directline" };
            var random             = new RandomGenerator();

            var connectionRequests = _exceptionHandler.Get(() => _routingDataManager.GetConnections());

            return Builder<Conversation>.CreateListOfSize(top)
                .All()
                    .With(o => o.ConversationInformation = Builder<ConversationInformation>.CreateNew()
                    .With(ci => ci.MessagesCount = random.Next(2, 30))
                    .With(ci => ci.SentimentScore = random.Next(0.0d, 1.0d))
                    .Build())
                    .With(o => o.ConversationReference = Builder<ConversationReference>.CreateNew()
                    .With(cr => cr.ChannelId = channels[random.Next(0, channels.Count())])
                    .With(cr => cr.User = Builder<ChannelAccount>.CreateNew().Build())
                    .With(cr => cr.Bot  = Builder<ChannelAccount>.CreateNew().Build())
                    .With(cr => cr.Conversation = Builder<ConversationAccount>.CreateNew().Build())
                    .Build())
                    .With(o => o.UserInformation = Builder<SampleUserInformation>.CreateNew()
                    .Build())
                .Build()
                .ToList();
        }


        public void TransmitMessageHistoryProactively(string channelId, string conversationId, string userId)
        {
            if (string.IsNullOrEmpty(channelId) || string.IsNullOrEmpty(conversationId) || string.IsNullOrEmpty(userId))
                return;

            var conversationRefence = _exceptionHandler.Get(() => 
                _routingDataManager
                    .FindConversationReference(channelId, conversationId)
            );

            if (conversationRefence == null)
                return;

            var messageLog = _conversationHistory.GetMessageLog(conversationRefence);

            //TODO: Grab hold of the bot and make it proactively transmit the messageLog
        }


        /// <summary>
        /// Sample UserInformation override class, used for demo purposes only
        /// </summary>
        private class SampleUserInformation : UserInformation
        {
            [JsonProperty(PropertyName = "isvip")]
            public bool IsVip { get; set; }


            [JsonProperty(PropertyName = "department")]
            public string Department { get; set; }
        }

    }
}
