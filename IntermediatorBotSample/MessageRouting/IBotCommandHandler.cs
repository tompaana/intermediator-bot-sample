using Microsoft.Bot.Connector;
using System.Threading.Tasks;

namespace MessageRouting
{
    public interface IBotCommandHandler
    {
        /// <summary>
        /// Some channels do not support mentions (@botname) and this method will return an
        /// alternative keyword for starting commands with.
        /// </summary>
        /// <returns>A special keyword to start commands with.</returns>
        string GetCommandKeyword();

        /// <summary>
        /// Handles the direct commands to the bot.
        /// </summary>
        /// <param name="activity">The activity containing a possible command.</param>
        /// <returns>True, if a command was detected and handled. False otherwise.</returns>
        Task<bool> HandleBotCommandAsync(Activity activity);
    }
}
