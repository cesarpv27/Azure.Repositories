using System;
using System.Collections.Generic;
using System.Text;

namespace AzStorage.Core.Utilities
{
    internal class ConstProvider
    {
        public const int maxDegreeOfParallelism = 5;
        public const string DefaultCosmosPartitionKeyPath = "/PartitionKey";
        public const string CosmosPartitionKeyPathStartPattern = "/";

        public const string DefaultQueryVarName = "c";
        public const string DefaultQueryPrefix = "select * from Container " + DefaultQueryVarName + " where ";
    }
}
