using Microsoft.Bot.Connector;
using System.Configuration;
using System.Linq;

namespace IntermediatorBot.Extensions
{
    public static class ActivityExtensions
    {
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