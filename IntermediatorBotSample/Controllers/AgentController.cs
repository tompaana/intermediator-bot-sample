using System;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;
using Underscore.Bot.MessageRouting;
using Underscore.Bot.MessageRouting.DataStore;
using Underscore.Bot.Models;

namespace IntermediatorBotSample.Controllers
{
    /// <summary>
    /// This class handles the direct requests made by the Agent UI component.
    /// </summary>
    public class AgentController : ApiController
    {
        private const string ResponseNone = "None";

        /// <summary>
        /// Handles requests sent by the Agent UI.
        /// If there are no aggregation channels set and one or more pending requests exist,
        /// the oldest request is processed and sent to the Agent UI.
        /// </summary>
        /// <param name="id">Not used.</param>
        /// <returns>The details of the user who made the request or "None", if no pending requests
        /// or if one or more aggregation channels are set up.</returns>
        [EnableCors("*", "*", "*")]
        public string GetAgentById(int id)
        {
            string response = ResponseNone;
            MessageRouterManager messageRouterManager = WebApiConfig.MessageRouterManager;
            IRoutingDataManager routingDataManager = messageRouterManager.RoutingDataManager;

            if (routingDataManager.GetAggregationParties().Count == 0
                && routingDataManager.GetPendingRequests().Count > 0)
            {
                try
                {
                    Party conversationClientParty = messageRouterManager.RoutingDataManager.GetPendingRequests().First();
                    messageRouterManager.RoutingDataManager.RemovePendingRequest(conversationClientParty);
                    response = conversationClientParty.ToJsonString();
                }
                catch (InvalidOperationException e)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to handle a pending request: {e.Message}");
                }
            }

            return response;
        }
    }
}