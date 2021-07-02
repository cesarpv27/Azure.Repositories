using System;
using Azure;
using Azure.Data.Tables;

namespace AzStorage.Test.Utilities
{
    public class SampleTableEntity : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
    }
}
