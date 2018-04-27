using IntermediatorBotSample.Models;
using Microsoft.Bot.Schema;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Underscore.Bot.MessageRouting.DataStore;
using Underscore.Bot.MessageRouting.DataStore.Azure;

//string rowKey = MessageRoutingUtils.GetChannelAccount(log.User).Id;
namespace IntermediatorBotSample.ConversationHistory
{
    public class ConversationHistory
    {
        private const string ConversationHistoryTableName = "ConversationHistory";
        private const string partitionKey = "botHandOff";
        private CloudTable _messageLogsTable;
        private IList<MessageLog> _inMemoryMessageLogs;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connectionString">The connection string for Azure Table Storage.</param>
        public ConversationHistory(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                Debug.WriteLine("WARNING!!! No connection string - storing message logs in memory");
                _inMemoryMessageLogs = new List<MessageLog>();
            }
            else
            {
                Debug.WriteLine("Using Azure Table Storage for storing message logs");
                _messageLogsTable = AzureStorageHelper.GetTable(connectionString, ConversationHistoryTableName);
                MakeSureConversationHistoryTableExistsAsync().RunSynchronously();
            }
        }

        /// <returns>All the message logs.</returns>
        public IList<MessageLog> GetMessageLogs()
        {
            if (_messageLogsTable != null)
            {
                var entities = GetAllEntitiesFromTable(_messageLogsTable).Result;
                return GetAllMessageLogsFromEntities(entities);
            }

            return _inMemoryMessageLogs;
        }

        /// <summary>
        /// Finds the message log associated with the given user.
        /// </summary>
        /// <param name="user">The user whose message log to find.</param>
        /// <returns>The message log of the user or null, if not found.</returns>
        public MessageLog GetMessageLog(ConversationReference user)
        {
            var messageLogs = GetMessageLogs();

            foreach (MessageLog messageLog in messageLogs)
            {
                if (RoutingDataManager.HasMatchingChannelAccounts(user, messageLog.User))
                {
                    return messageLog;
                }
            }
            return null;
        }

        /// <summary>
        /// Adds a message (an activity) to the log associated with the given user.
        /// </summary>
        /// <param name="activity">The activity to add.</param>
        /// <param name="user">The user associated with the message.</param>
        public void AddMessageLog(Microsoft.Bot.Schema.Activity activity, ConversationReference user)
        {
            if (_messageLogsTable != null)
            {
                // Add to AzureTable
            }

            else
            {
                // Add to InMemory storage
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user">The user whose message log to delete.</param>
        public void DeleteMessageLog(ConversationReference user)
        {
            // TODO
        }

        /// <summary>
        /// Makes sure the cloud table for storing message logs exists.
        /// A table is created, if one doesn't exist.
        /// </summary>
        private async Task MakeSureConversationHistoryTableExistsAsync()
        {
            try
            {
                await _messageLogsTable.CreateIfNotExistsAsync();
                System.Diagnostics.Debug.WriteLine($"Table '{_messageLogsTable.Name}' created or did already exist");
            }
            catch (StorageException e)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create table '{_messageLogsTable.Name}' (perhaps it already exists): {e.Message}");
            }
        }

        private async Task<IList<MessageLogEntity>> GetAllEntitiesFromTable(CloudTable table)
        {
            var query = new TableQuery<MessageLogEntity>()
                .Where(TableQuery.GenerateFilterCondition(
                    "PartitionKey", QueryComparisons.Equal, partitionKey));
            return await table.ExecuteTableQueryAsync(query);
        }

        private List<MessageLog> GetAllMessageLogsFromEntities(IList<MessageLogEntity> entities)
        {
            var messageLogs = new List<MessageLog>();
            foreach (var entity in entities)
            {
                var messageLog = JsonConvert.DeserializeObject<MessageLog>(entity.Body);
                messageLogs.Add(messageLog);
            }
            return messageLogs;
        }
    }
}