using System;
using System.Collections.Generic;
using System.Text;
using AzStorage.Core.Utilities;
using Newtonsoft.Json;

namespace AzStorage.Core.Cosmos
{
    public abstract class BaseCosmosEntity
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        public string PartitionKey { get; set; }
    }
}
