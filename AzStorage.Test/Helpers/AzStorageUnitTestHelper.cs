using AzCoreTools.Core;
using AzCoreTools.Utilities;
using AzStorage.Repositories;
using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Linq;
using AzCoreTools.Core.Validators;
using AzStorage.Core.Tables;
using System.Threading.Tasks;
using CoreTools.Extensions;

namespace AzStorage.Test.Helpers
{
    internal class AzStorageUnitTestHelper : UnitTestHelper
    {
        private static string GetStorageConnectionString()
        {
            string storageConnectionString;
            TestEnvironment.TryGetStorageConnectionString(out storageConnectionString);

            return storageConnectionString;
        }

        private static string _storageConnectionString;
        protected static string StorageConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(_storageConnectionString))
                    _storageConnectionString = GetStorageConnectionString();

                return _storageConnectionString;
            }
        }
    }
}
