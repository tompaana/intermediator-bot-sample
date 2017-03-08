using Microsoft.Bot.Connector;
using System.Threading.Tasks;

namespace MessageRouting
{
    public class Commands
    {
        // Using nomenclature from Node.js: https://github.com/palindromed/Bot-HandOff/blob/master/commands.ts
        public const string CommandKeyword = "agent"; // Used if the channel does not support mentions
        public const string CommandAddAggregationChannel = "watch";
        public const string CommandAcceptRequest = "connect";
        public const string CommandRejectRequest = "reject";
        public const string CommandEndEngagement = "disconnect";
        public const string CommandDeleteAllRoutingData = "reset";
        public const string CommandListOptions = "options";

        // For debugging
        public const string CommandListAllParties = "list parties";
        public const string CommandListPendingRequests = "waiting";
        public const string CommandListEngagements = "list";
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
