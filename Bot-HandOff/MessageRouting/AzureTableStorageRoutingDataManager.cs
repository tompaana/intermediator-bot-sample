using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Bot.Connector;

namespace MessageRouting
{
    /// <summary>
    /// Routing data manager that stores the data in Azure Table storage services.
    /// Caching policy: If the local query finds nothing, update the data from the storage.
    /// See IRoutingDataManager for general documentation of properties and methods.
    /// 
    /// NOTE: DO NOT USE THIS CLASS - THIS IS NOT FAR FROM A PLACEHOLDER CURRENTLY
    /// </summary>
    public class AzureTableStorageRoutingDataManager : LocalRoutingDataManager
    {
        private string _connectionString;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connectionString">The connection string for the Azure Table storage services.</param>
        public AzureTableStorageRoutingDataManager(string connectionString) : base()
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException($"Connection string ({nameof(connectionString)}) cannot be empty or null");
            }

            _connectionString = connectionString;
        }

        public override bool AddParty(Party newParty, bool isUser = true)
        {
            return base.AddParty(newParty, isUser);
        }
    }
}