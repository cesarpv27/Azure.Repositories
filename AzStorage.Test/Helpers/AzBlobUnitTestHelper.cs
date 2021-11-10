using AzCoreTools.Core;
using AzCoreTools.Core.Interfaces;
using AzCoreTools.Utilities;
using AzStorage.Core.Blobs;
using AzStorage.Repositories;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CoreTools.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

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
        
        public static List<string> GetRandomBlobNamesFromDefault(int namesAmount)
        {
            if (namesAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(namesAmount));

            var resultingNames = new List<string>(namesAmount);
            for (int i = 0; i < namesAmount; i++)
                resultingNames.Add(GetRandomBlobNameFromDefault(-1));

            return resultingNames;
        }

        public static void AssertExpectedSuccessfulGenResponse(
            IAzDetailedResponse<List<BlobContainerItem>> response, 
            List<string> blobContainerNames)
        {
            AssertExpectedSuccessfulGenResponse(response);
            var blobContainerNamesInResponse = response.Value.Select(resp => resp.Name);
            foreach (var _blobContainerName in blobContainerNames)
                Assert.Contains(_blobContainerName, blobContainerNamesInResponse);
        }

        public static void AssertDeleteBlobContainerExpectedSuccessfulResponses(
            List<AzStorageResponse<BlobContainerClient>> responses)
        {
            int count = 0;
            var deleteBlobContainerAsyncResponses = new List<AzStorageResponse>(responses.Count);
            while (count < responses.Count)
            {
                if (responses[count].Succeeded)
                    deleteBlobContainerAsyncResponses.Add(DeleteBlobContainerAsync(responses[count].Value.Name)
                        .WaitAndUnwrapException());
                count++;
            }

            AssertExpectedSuccessfulResponses(deleteBlobContainerAsyncResponses);
        }

        #endregion

        #region AzBlobRepository methods

        #region Blob container operations

        public static AzStorageResponse<BlobContainerClient> CreateBlobContainer(
            CreateBlobContainerMetadata blobContainerMetadata,
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzBlobRepository(optionCreateIfNotExist)
                .CreateBlobContainer(blobContainerMetadata, cancellationToken);
        }

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
        
        public static List<AzStorageResponse<BlobContainerClient>> CreateBlobContainers(
            IEnumerable<CreateBlobContainerMetadata> blobContainersMetadata,
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzBlobRepository(optionCreateIfNotExist)
                .CreateBlobContainers(blobContainersMetadata, cancellationToken);
        }
        
        public static List<AzStorageResponse<BlobContainerClient>> CreateBlobContainers(
            IEnumerable<string> blobContainerNames,
            PublicAccessType publicAccessType = PublicAccessType.None,
            IDictionary<string, string> metadata = null,
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzBlobRepository(optionCreateIfNotExist)
                .CreateBlobContainers(blobContainerNames, publicAccessType, metadata, cancellationToken);
        }

        public static AzStorageResponse<List<BlobContainerItem>> GetBlobContainers(
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

        public static AzStorageResponse<List<BlobContainerItem>> GetAllBlobContainers(
            int take,
            BlobContainerTraits traits = BlobContainerTraits.None,
            BlobContainerStates states = BlobContainerStates.None,
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzBlobRepository(optionCreateIfNotExist)
                .GetAllBlobContainers(take, traits, states, cancellationToken);
        }

        public static AzStorageResponse<List<BlobContainerItem>> GetAllBlobContainers(
            BlobContainerTraits traits = BlobContainerTraits.None,
            BlobContainerStates states = BlobContainerStates.None,
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzBlobRepository(optionCreateIfNotExist)
                .GetAllBlobContainers(traits, states, cancellationToken);
        }

        public static AzStorageResponse DeleteBlobContainer(
            DeleteBlobContainerMetadata blobContainerName,
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzBlobRepository(optionCreateIfNotExist)
                .DeleteBlobContainer(blobContainerName, cancellationToken);
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
        
        public static List<AzStorageResponse<string>> DeleteBlobContainers(
            IEnumerable<string> blobContainerNames,
            BlobRequestConditions conditions = null,
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzBlobRepository(optionCreateIfNotExist)
                .DeleteBlobContainers(blobContainerNames, conditions, cancellationToken);
        }

        public static List<AzStorageResponse<DeleteBlobContainerMetadata>> DeleteBlobContainers(
            IEnumerable<DeleteBlobContainerMetadata> blobContainerNames,
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzBlobRepository(optionCreateIfNotExist)
                .DeleteBlobContainers(blobContainerNames, cancellationToken);
        }

        public static List<AzStorageResponse<string>> DeleteAllBlobContainers(
            BlobContainerTraits traits = BlobContainerTraits.None,
            BlobContainerStates states = BlobContainerStates.None,
            BlobRequestConditions conditions = null,
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return GetOrCreateAzBlobRepository(optionCreateIfNotExist)
                .DeleteAllBlobContainers(traits, states, conditions, cancellationToken);
        }

        #endregion

        #region Blob container async operations

        public static async Task<AzStorageResponse<BlobContainerClient>> CreateBlobContainerAsync(
            CreateBlobContainerMetadata blobContainerMetadata,
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

        public static async Task<List<AzStorageResponse<BlobContainerClient>>> CreateBlobContainersAsync(
            IEnumerable<string> blobContainerNames,
            PublicAccessType publicAccessType = PublicAccessType.None,
            IDictionary<string, string> metadata = null,
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzBlobRepository(optionCreateIfNotExist)
                .CreateBlobContainersAsync(blobContainerNames, publicAccessType, metadata, cancellationToken);
        }

        public static async Task<List<AzStorageResponse<BlobContainerClient>>> CreateBlobContainersAsync(
            IEnumerable<CreateBlobContainerMetadata> blobContainersMetadata,
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzBlobRepository(optionCreateIfNotExist)
                .CreateBlobContainersAsync(blobContainersMetadata, cancellationToken);
        }

        public static async Task<AzStorageResponse<List<BlobContainerItem>>> GetBlobContainersAsync(
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
        
        public static async Task<AzStorageResponse<List<BlobContainerItem>>> GetAllBlobContainersAsync(
            int take,
            BlobContainerTraits traits = BlobContainerTraits.None,
            BlobContainerStates states = BlobContainerStates.None,
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzBlobRepository(optionCreateIfNotExist)
                .GetAllBlobContainersAsync(take, traits, states, cancellationToken);
        }
        
        public static async Task<AzStorageResponse<List<BlobContainerItem>>> GetAllBlobContainersAsync(
            BlobContainerTraits traits = BlobContainerTraits.None,
            BlobContainerStates states = BlobContainerStates.None,
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzBlobRepository(optionCreateIfNotExist)
                .GetAllBlobContainersAsync(traits, states, cancellationToken);
        }

        public static async Task<AzStorageResponse> DeleteBlobContainerAsync(
            DeleteBlobContainerMetadata blobContainerName,
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzBlobRepository(optionCreateIfNotExist)
                .DeleteBlobContainerAsync(blobContainerName, cancellationToken);
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

        public static async Task<List<AzStorageResponse<string>>> DeleteBlobContainersAsync(
            IEnumerable<string> blobContainerNames,
            BlobRequestConditions conditions = null,
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzBlobRepository(optionCreateIfNotExist)
                .DeleteBlobContainersAsync(blobContainerNames, conditions, cancellationToken);
        }
        
        public static async Task<List<AzStorageResponse<DeleteBlobContainerMetadata>>> DeleteBlobContainersAsync(
            IEnumerable<DeleteBlobContainerMetadata> blobContainerNames,
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzBlobRepository(optionCreateIfNotExist)
                .DeleteBlobContainersAsync(blobContainerNames, cancellationToken);
        }
        
        public static async Task<List<AzStorageResponse<string>>> DeleteAllBlobContainersAsync(
            BlobContainerTraits traits = BlobContainerTraits.None,
            BlobContainerStates states = BlobContainerStates.None,
            BlobRequestConditions conditions = null,
            CancellationToken cancellationToken = default,
            CreateResourcePolicy optionCreateIfNotExist = CreateResourcePolicy.OnlyFirstTime)
        {
            return await GetOrCreateAzBlobRepository(optionCreateIfNotExist)
                .DeleteAllBlobContainersAsync(traits, states, conditions, cancellationToken);
        }

        #endregion

        #endregion
    }
}
