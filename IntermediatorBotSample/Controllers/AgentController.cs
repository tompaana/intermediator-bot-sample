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
                Party party = messageRouterManager.RoutingDataManager.GetPendingRequests().Last();
                messageRouterManager.RoutingDataManager.RemovePendingRequest(party);
                return party.GetConversationId();
            }

            return ResponseNone;
        }

        [HttpGet]
        public string GetNextConvForConsult()
        {
            return "hello";
        }
    }
}