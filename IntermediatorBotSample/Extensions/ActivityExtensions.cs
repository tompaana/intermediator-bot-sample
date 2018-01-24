using IntermediatorBotSample;
using Microsoft.Bot.Connector;
using System.Collections.Generic;
using System.Linq;

namespace IntermediatorBot.Extensions
{
    public static class ActivityExtensions
    {
        /// <summary>
        /// Checks to see if the ChannelId property on the activity is contained within the list
        /// of allowed channels for use by the agents. If there is no setting, all channel IDs are
        /// considered permitted.
        /// </summary>
        /// <param name="activity">The activity to check.</param>
        /// <returns>True, if the channel ID of the activity is included in the permitted aggregation channels. False otherwise.</returns>
        public static bool IsFromPermittedAgentChannel(this Activity activity)
        {
            string[] permittedAggregationChannelsAsStringArray = WebApiConfig.Settings.PermittedAggregationChannels;
            IList<string> permittedAggregationChannels = permittedAggregationChannelsAsStringArray?.ToList();

            if (permittedAggregationChannels != null
                && permittedAggregationChannels.Any()
                && !permittedAggregationChannels.Contains(activity.ChannelId.ToLower()))
            {
                return false;
            }

            return true;
        }
    }
}