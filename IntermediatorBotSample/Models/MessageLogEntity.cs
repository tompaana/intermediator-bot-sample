using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IntermediatorBotSample.Models
{
    public class MessageLogEntity:TableEntity
    {
        public string Body { get; set; }
    }
}
