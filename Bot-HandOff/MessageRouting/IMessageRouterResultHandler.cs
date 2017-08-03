using System;
using System.Threading.Tasks;

namespace MessageRouting
{
    /// <summary>
    /// Implement this interface to customize how the bot reacts to the results of the message
    /// router manager.
    /// </summary>
    public interface IMessageRouterResultHandler
    {
        /// <summary>
        /// Handles the given message router result.
        /// Note: This method needs to be async so that it can be awaited.
        /// </summary>
        /// <param name="messageRouterResult">The result to handle.</param>
        Task HandleResultAsync(MessageRouterResult messageRouterResult);
    }
}
