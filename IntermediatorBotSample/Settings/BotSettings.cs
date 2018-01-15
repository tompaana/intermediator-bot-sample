using System;
using System.Configuration;
using System.Linq;

namespace IntermediatorBotSample.Settings
{
    /// <summary>
    /// Provider of the bot settings.
    /// </summary>
    public class BotSettings
    {
        // Setting keys
        public static readonly string KeyBotId = "BotId";
        public static readonly string KeyMicrosoftAppId = "MicrosoftAppId";
        public static readonly string KeyMicrosoftAppPassword = "MicrosoftAppPassword";
        public static readonly string KeyRoutingDataStorageConnectionString = "RoutingDataStorageConnectionString";
        public static readonly string KeyRejectConnectionRequestIfNoAggregationChannel = "RejectConnectionRequestIfNoAggregationChannel";
        public static readonly string KeyPermittedAggregationChannels = "PermittedAggregationChannels";

        /// <summary>
        /// Tries to resolve a setting value by the given key.
        /// </summary>
        /// <param name="key">The key of a setting.</param>
        /// <returns>A string containing the value or null, if no setting found.</returns>
        public string this[string key]
        {
            get
            {
                if (string.IsNullOrEmpty(key))
                {
                    throw new ArgumentNullException("Key cannot be null or empty");
                }

                string settingValue = null;

                if (ConfigurationManager.AppSettings.AllKeys.Contains(key))
                {
                    settingValue = ConfigurationManager.AppSettings[key];
                }

                if (string.IsNullOrEmpty(settingValue))
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to find a setting value by key \"{key}\"");
                }

                return settingValue;
            }
        }

        public bool RejectConnectionRequestIfNoAggregationChannel
        {
            get
            {
                string settingValueAsString = this[KeyRejectConnectionRequestIfNoAggregationChannel];

                if (!string.IsNullOrEmpty(settingValueAsString)
                    && settingValueAsString.ToLower().Trim().Equals("true"))
                {
                    return true;
                }

                return false;
            }
        }

        public string[] PermittedAggregationChannels
        {
            get
            {
                string settingValueAsString = this[KeyPermittedAggregationChannels];

                if (!string.IsNullOrEmpty(settingValueAsString))
                {
                    string[] permittedAggregationChannels = settingValueAsString.Split(',');

                    for (int i = 0; i < permittedAggregationChannels.Length; ++i)
                    {
                        permittedAggregationChannels[i] = permittedAggregationChannels[i].Trim();
                    }
                }

                return null;
            }
        }
    }
}