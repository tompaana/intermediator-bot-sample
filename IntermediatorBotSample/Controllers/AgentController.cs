using MessageRouting;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;

namespace IntermediatorBot.Controllers
{
    public class AgentController : ApiController
    {
        private const string ResponseNone = "None";

        [EnableCors("*", "*", "*")]
        public string GetAgentById(int Id)
        {
            MessageRouterManager messageRouterManager = MessageRouterManager.Instance;

            if (messageRouterManager.RoutingDataManager.GetPendingRequests().Count > 0)
            {
                Party conversationClientParty = messageRouterManager.RoutingDataManager.GetPendingRequests().Last();
                messageRouterManager.RoutingDataManager.RemovePendingRequest(conversationClientParty);
                return conversationClientParty.ToIdString();
            }

            return ResponseNone;
        }
    }
}