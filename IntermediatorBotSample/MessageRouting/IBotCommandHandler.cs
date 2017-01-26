using Microsoft.Bot.Connector;
using System.Threading.Tasks;

namespace MessageRouting
{
    public class Commands
    {
        public const string CommandKeyword = "command"; // Used if the channel does not support mentions
        public const string CommandAddAggregationChannel = "add aggregation";
        public const string CommandAcceptRequest = "accept";
        public const string CommandRejectRequest = "reject";
        public const string CommandEndEngagement = "end";
        public const string CommandDeleteAllRoutingData = "reset";

        // For debugging
        public const string CommandEnableAggregation = "enable aggregation";
        public const string CommandDisableAggregation = "disable aggregation";
        public const string CommandListAllParties = "list parties";
        public const string CommandListPendingRequests = "list requests";
        public const string CommandListEngagements = "list conversations";
#if DEBUG
        public const string CommandListLastMessageRouterResults = "list results";
#endif
    }

    public interface IBotCommandHandler
    {
        /// <summary>
        /// Handles the direct commands to the bot.
        /// </summary>
        /// <param name="activity">The activity containing a possible command.</param>
        /// <returns>True, if a command was detected and handled. False otherwise.</returns>
        Task<bool> HandleCommandAsync(Activity activity);
    }
}
