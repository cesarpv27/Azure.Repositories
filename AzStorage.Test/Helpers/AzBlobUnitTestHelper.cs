using AzCoreTools.Core;
using AzStorage.Core.Blobs;
using AzStorage.Repositories;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace AzStorage.Test.Helpers
{
    internal class AzBlobUnitTestHelper : AzStorageUnitTestHelper
    {
        #region Miscellaneous methods

        private static AzBlobRepository _AzBlobRepository;
        public static AzBlobRepository GetOrCreateAzBlobRepository(CreateResourcePolicy optionCreateIfNotExist)
        {
            if (_AzBlobRepository == null)
                _AzBlobRepository = CreateAzBlobRepository(optionCreateIfNotExist);

            return _AzBlobRepository;
        }

        private static AzBlobRepository CreateAzBlobRepository(CreateResourcePolicy optionCreateIfNotExist)
        {
            var _azBlobRetryOptions = new AzBlobRetryOptions
            {
                Mode = Azure.Core.RetryMode.Fixed,
                Delay = new TimeSpan(0, 0, 1),
                MaxDelay = new TimeSpan(0, 1, 0),
                NetworkTimeout = new TimeSpan(0, 3, 0),
                MaxRetries = 5,
            };

            return new AzBlobRepository(StorageConnectionString, 
                createTableResource: optionCreateIfNotExist, 
                retryOptions: _azBlobRetryOptions);
        }

        public static string GetDefaultBlobName => "sampleblob";

        public static string GetRandomBlobNameFromDefault(int randomValue = -1)
        {
            return BuildRandomName(GetDefaultBlobName, randomValue);
        }

        #endregion

        #region AzBlobRepository methods

        public static AzStorageResponse<BlobContainerClient> CreateBlobContainer(
            string blobContainerName,
            PublicAccessType publicAccessType = PublicAccessType.None,
            IDictionary<string, string> metadata = null,
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {

            return GetOrCreateAzBlobRepository(optionCreateIfNotExist)
                .CreateBlobContainer(blobContainerName, publicAccessType, metadata, cancellationToken);
        }
        
        public static AzStorageResponse DeleteBlobContainer(
            string blobContainerName, 
            BlobRequestConditions conditions = null, 
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzBlobRepository(optionCreateIfNotExist)
                .DeleteBlobContainer(blobContainerName, conditions, cancellationToken);
        }

        #endregion
    }
}
