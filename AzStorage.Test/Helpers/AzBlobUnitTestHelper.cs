using AzCoreTools.Core;
using AzCoreTools.Utilities;
using AzStorage.Core.Blobs;
using AzStorage.Repositories;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        #region Blob container operations

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

        public virtual AzStorageResponse<List<BlobContainerItem>> GetBlobContainers(
           BlobContainerTraits traits = BlobContainerTraits.None,
           BlobContainerStates states = BlobContainerStates.None,
           string prefix = null,
           CancellationToken cancellationToken = default,
           int take = ConstProvider.DefaultTake,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzBlobRepository(optionCreateIfNotExist)
                .GetBlobContainers(traits, states, prefix, cancellationToken, take);
        }

        #endregion

        #region Blob container async operations

        public static async Task<AzStorageResponse<BlobContainerClient>> CreateBlobContainerAsync(
            BlobContainerMetadata blobContainerMetadata,
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {

            return await GetOrCreateAzBlobRepository(optionCreateIfNotExist)
                .CreateBlobContainerAsync(blobContainerMetadata, cancellationToken);
        }

        public static async Task<AzStorageResponse<BlobContainerClient>> CreateBlobContainerAsync(
            string blobContainerName,
            PublicAccessType publicAccessType = PublicAccessType.None,
            IDictionary<string, string> metadata = null,
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {

            return await GetOrCreateAzBlobRepository(optionCreateIfNotExist)
                .CreateBlobContainerAsync(blobContainerName, publicAccessType, metadata, cancellationToken);
        }

        public static async Task<AzStorageResponse> DeleteBlobContainerAsync(
            string blobContainerName,
            BlobRequestConditions conditions = null,
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzBlobRepository(optionCreateIfNotExist)
                .DeleteBlobContainerAsync(blobContainerName, conditions, cancellationToken);
        }

        public virtual async Task<AzStorageResponse<List<BlobContainerItem>>> GetBlobContainersAsync(
           BlobContainerTraits traits = BlobContainerTraits.None,
           BlobContainerStates states = BlobContainerStates.None,
           string prefix = null,
           CancellationToken cancellationToken = default,
           int take = ConstProvider.DefaultTake,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzBlobRepository(optionCreateIfNotExist)
                .GetBlobContainersAsync(traits, states, prefix, cancellationToken, take);
        }

        #endregion

        #endregion
    }
}
