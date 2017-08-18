using Microsoft.Bot.Connector;
using System.Configuration;
using System.Linq;

namespace IntermediatorBot.Extensions
{
    public static class ActivityExtensions
    {
        /// <summary>
        /// Checks to see if the ChannelId property on the activity is contained within the list of allowed
        /// channels for use by agents using the PermittedAgentsChannel setting in the web.config. If the app setting is empty
        /// or missing then the method returns true
        /// </summary>
        /// <param name="activity">The activity to check</param>
        /// <returns></returns>
        public static bool IsFromPermittedAgentChannel(this Activity activity)
        {
            var permittedAgentChannels = ConfigurationManager.AppSettings.AllKeys.Contains("PermittedAgentChannels") ? ConfigurationManager.AppSettings["PermittedAgentChannels"] : null;
            var permittedAgentChannelsList = !string.IsNullOrEmpty(permittedAgentChannels) ? permittedAgentChannels.ToLower().Split(',').ToList() : null;

            if(permittedAgentChannels != null && permittedAgentChannels.Any())
            {
                if (!permittedAgentChannels.Contains(activity.ChannelId.ToLower()))
                    return false;
            }

            return true;
        }
    }
}