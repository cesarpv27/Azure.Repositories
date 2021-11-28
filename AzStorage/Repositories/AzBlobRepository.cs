using AzCoreTools.Core;
using AzStorage.Core.Blobs;
using AzStorage.Repositories.Core;
using ExThrower = CoreTools.Throws.ExceptionThrower;
using System;
using System.Collections.Generic;
using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Threading;
using AzCoreTools.Helpers;
using Azure;
using System.Threading.Tasks;
using AzCoreTools.Utilities;
using AzCoreTools.Extensions;
using AzStorageConstProvider = AzStorage.Core.Utilities.ConstProvider;
using AzStorage.Core.Texting;
using System.Linq;
using System.IO;
using AzStorage.Core.Errors.Blob;

namespace AzStorage.Repositories
{
    public class AzBlobRepository : AzStorageRepository<AzBlobRetryOptions>
    {
        protected AzBlobRepository() { }

        public AzBlobRepository(string connectionString,
            string blobContainerName = default,
            string blobName = default,
            CreateResourcePolicy createResourcePolicy = CreateResourcePolicy.OnlyFirstTime,
            AzBlobRetryOptions retryOptions = null) : base(createResourcePolicy, retryOptions)
        {
            ThrowIfInvalidConnectionString(connectionString);

            ConnectionString = connectionString;
            BlobContainerName = blobContainerName;
            BlobName = blobName;

            InitializeForcingNotMandatory();
        }

        #region Private properties

        private BlobErrorManager _BlobErrorManager;
        private BlobErrorManager BlobErrorManager
        {
            get
            {
                if (_BlobErrorManager == null)
                    _BlobErrorManager = new BlobErrorManager();

                return _BlobErrorManager;
            }
        }

        #endregion

        #region Protected properties

        protected virtual string BlobServiceClientAbsoluteUri { get; set; }

        protected virtual BlobServiceClient BlobServiceClient { get; set; }

        protected virtual BlobContainerClient BlobContainerClient { get; set; }

        protected virtual string BlobContainerName { get; set; }

        protected virtual BlobClient BlobClient { get; set; }

        protected virtual string BlobName { get; set; }

        protected virtual bool IsFirstTimeBlobContainerClientCreation { get; set; } = true;
        protected virtual bool IsFirstTimeBlobClientCreation { get; set; } = true;

        #endregion

        #region Validators

        protected virtual bool ValidateBlobServiceClient(BlobServiceClient blobServiceClient)
        {
            return blobServiceClient != default;
        }

        protected virtual bool ValidateBlobContainerName(string blobContainerName)
        {
            return !string.IsNullOrEmpty(blobContainerName) && !string.IsNullOrWhiteSpace(blobContainerName);
        }

        protected virtual bool ValidateBlobContainerMetadata(BlobContainerMetadata blobContainerMetadata)
        {
            if (blobContainerMetadata == default)
                return false;

            return ValidateBlobContainerName(blobContainerMetadata.BlobContainerName);
        }

        protected virtual bool ValidateBlobContainerClient(BlobContainerClient blobContainerClient)
        {
            return blobContainerClient != default;
        }

        protected virtual bool ValidateBlobName(string blobName)
        {
            return !string.IsNullOrEmpty(blobName) && !string.IsNullOrWhiteSpace(blobName);
        }

        protected virtual bool ValidateBlobClient(BlobClient blobClient)
        {
            return blobClient != default;
        }

        #endregion

        #region Throws

        protected virtual void ThrowIfInvalidBlobServiceClient()
        {
            if (!ValidateBlobServiceClient(GetBlobServiceClient()))
                ExThrower.ST_ThrowNullReferenceException(nameof(BlobServiceClient));
        }

        protected virtual void ThrowIfInvalidBlobContainerName(string blobContainerName)
        {
            if (!ValidateBlobContainerName(blobContainerName))
                ExThrower.ST_ThrowNullReferenceException(nameof(blobContainerName));
        }

        protected virtual void ThrowIfInvalidBlobContainerMetadata(BlobContainerMetadata blobContainerMetadata)
        {
            if (!ValidateBlobContainerMetadata(blobContainerMetadata))
                ExThrower.ST_ThrowArgumentException(nameof(blobContainerMetadata), nameof(blobContainerMetadata));
        }

        protected virtual void ThrowIfInvalidBlobContainerClient()
        {
            if (!ValidateBlobContainerClient(BlobContainerClient))
                ExThrower.ST_ThrowNullReferenceException(nameof(BlobContainerClient));
        }

        protected virtual void ThrowIfInvalidBlobName(string blobName)
        {
            if (!ValidateBlobName(blobName))
                ExThrower.ST_ThrowNullReferenceException(nameof(blobName));
        }

        protected virtual void ThrowIfInvalidBlobClient()
        {
            if (!ValidateBlobClient(BlobClient))
                ExThrower.ST_ThrowNullReferenceException(nameof(BlobClient));
        }

        #endregion

        #region Protected & private methods

        protected virtual void InitializeForcingNotMandatory()
        {
            InitializeForcingNotMandatory(BlobContainerName, BlobName);
        }

        protected virtual void InitializeForcingNotMandatory(
            string blobContainerName,
            string blobName)
        {
            CreateOrLoadBlobServiceClient();
            Initialize(blobContainerName, blobName, false, false);
        }

        private void Initialize(
            string blobContainerName,
            string blobName,
            bool mandatoryBlobContainerName,
            bool mandatoryBlobName)
        {
            if (mandatoryBlobContainerName || !string.IsNullOrEmpty(blobContainerName))
                CreateOrLoadBlobContainerClient(blobContainerName);

            if (mandatoryBlobName || !string.IsNullOrEmpty(blobName))
                CreateOrLoadBlobClient(blobName);
        }

        private bool TryInitialize(
            string blobContainerName,
            string blobName,
            bool mandatoryBlobContainerName,
            bool mandatoryBlobName,
            out AzStorageResponse azStorageResponse)
        {
            try
            {
                Initialize(blobContainerName, blobName, mandatoryBlobContainerName, mandatoryBlobName);
            }
            catch (Exception e)
            {
                azStorageResponse = AzStorageResponse.Create(e);
                return false;
            }

            azStorageResponse = null;
            return true;
        }

        private List<AzStorageResponse> GetAzStorageResponseList(int capacity = 0)
        {
            return new List<AzStorageResponse>(capacity);
        }

        private List<AzStorageResponse<T>> GetAzStorageResponseList<T>(int capacity = 0)
        {
            return new List<AzStorageResponse<T>>(capacity);
        }

        private AzStorageResponse GetAzStorageResponseWithException<TEx>(TEx e)
            where TEx : Exception
        {
            return AzStorageResponse.Create(e);
        }

        private AzStorageResponse<T> GetAzStorageResponseWithException<T, TEx>(TEx e)
            where TEx : Exception
        {
            return AzStorageResponse<T>.CreateWithException<TEx, AzStorageResponse<T>>(e);
        }

        #endregion

        #region BlobServiceClient creator

        /// <summary>
        /// Initialize a BlobServiceClient instance with the value of ConnectionString.
        /// </summary>
        protected virtual void CreateOrLoadBlobServiceClient()
        {
            ThrowIfInvalidConnectionString();

            CreateOrLoadBlobServiceClient(ConnectionString);
        }

        /// <summary>
        /// Initialize a BlobServiceClient property with an instance using the value of connectionString.
        /// </summary>
        /// <param name="connectionString">A connection string includes the authentication information required for your
        /// application to access data in an Azure Storage account at runtime.</param>
        protected virtual void CreateOrLoadBlobServiceClient(string connectionString)
        {
            if (!ValidateBlobServiceClient(GetBlobServiceClient())
                || string.IsNullOrEmpty(BlobServiceClientAbsoluteUri)
                || !BlobServiceClientAbsoluteUri.Equals(GetBlobServiceClient().Uri?.AbsoluteUri))
            {
                BlobServiceClient = new BlobServiceClient(connectionString);
                BlobServiceClientAbsoluteUri = GetBlobServiceClient().Uri?.AbsoluteUri;
            }
        }

        protected virtual BlobServiceClient GetBlobServiceClient()
        {
            return BlobServiceClient;
        }

        #endregion

        #region BlobContainerClient creators

        /// <summary>
        /// Check if a container exists and create if does not exists.
        /// </summary>
        /// <param name="blobContainerName">The name of the container to create or load.</param>
        protected virtual void CreateOrLoadBlobContainerClient(string blobContainerName)
        {
            ThrowIfInvalidBlobContainerName(blobContainerName);
            ThrowIfInvalidBlobServiceClient();

            CreateOrLoadBlobContainerClient(blobContainerName, CreateBlobContainerClient, GetBlobContainerClient);
        }

        private void CreateOrLoadBlobContainerClient(
            string blobContainerName, 
            Func<dynamic[], BlobContainerClient> funcCreateResource,
            Func<dynamic[], BlobContainerClient> funcGetResource)
        {
            if (!ValidateBlobContainerClient(BlobContainerClient) || string.IsNullOrEmpty(BlobContainerName)
                || !BlobContainerName.Equals(blobContainerName))
                IsFirstTimeBlobContainerClientCreation = true;

            bool _isFirstTime = IsFirstTimeBlobContainerClientCreation;

            bool result = false;
            result = TryCreateOrGetResource(funcCreateResource, new dynamic[] { blobContainerName },
                funcGetResource, new dynamic[] { blobContainerName },
                BlobErrorManager.ExceptionContainsContainerAlreadyExistsAzError,
                ref _isFirstTime, out BlobContainerClient _blobContainerClientResponse);
            
            IsFirstTimeBlobContainerClientCreation = _isFirstTime;

            if (result && ValidateBlobContainerClient(_blobContainerClientResponse))
            {
                BlobContainerClient = _blobContainerClientResponse;
                BlobContainerName = blobContainerName;
            }
        }

        private BlobContainerClient CreateBlobContainerClient(dynamic[] @params)
        {
            return CreateBlobContainerClient(@params[0]);
        }

        private BlobContainerClient CreateBlobContainerClient(string blobContainerName)
        {
            return GetBlobServiceClient().CreateBlobContainer(blobContainerName);
        }
        
        private BlobContainerClient GetBlobContainerClient(dynamic[] @params)
        {
            return GetBlobContainerClient(@params[0]);
        }

        private BlobContainerClient GetBlobContainerClient(string blobContainerName)
        {
            return GetBlobServiceClient().GetBlobContainerClient(blobContainerName);
        }

        #endregion

        #region BlobClient

        /// <summary>
        /// Check if a blob exists and create if does not exists.
        /// </summary>
        /// <param name="blobName">The name of the blob to create or load.</param>
        protected virtual void CreateOrLoadBlobClient(string blobName)
        {
            ThrowIfInvalidBlobName(blobName);

            ThrowIfInvalidBlobContainerClient();

            CreateOrLoadBlobClient(blobName, CreateBlobClient);
        }

        private void CreateOrLoadBlobClient(string blobName, Func<dynamic[], BlobClient> func)
        {
            if (!ValidateBlobClient(BlobClient) || !ValidateBlobName(BlobName)
                || !BlobName.Equals(blobName))
                IsFirstTimeBlobClientCreation = true;

            bool _isFirstTime = IsFirstTimeBlobClientCreation;

            BlobClient _blobClientResponse;
            var result = TryCreateResource(func, new dynamic[] { blobName },
                ref _isFirstTime, out _blobClientResponse);

            IsFirstTimeBlobClientCreation = _isFirstTime;

            if (result && ValidateBlobClient(_blobClientResponse))
            {
                BlobClient = _blobClientResponse;
                BlobName = blobName;
            }
        }

        private BlobClient CreateBlobClient(dynamic[] @params)
        {
            return CreateBlobClient(@params[0]);
        }

        private BlobClient CreateBlobClient(string blobName)
        {
            //if (BlobContainerClient.exist)
            return BlobContainerClient.GetBlobClient(blobName);
        }

        private BlobClient GetBlobClient()
        {
            return BlobClient;
        }

        #endregion

        #region Container

        /// <summary>
        /// Creates a new blob container under the specified account. If a container
        /// with the same name of the property BlobContainerName of <paramref name="blobContainerMetadata"/> already exists, 
        /// the operation fails.
        /// </summary>
        /// <typeparam name="TIn">A custom model type.</typeparam>
        /// <param name="blobContainerMetadata">Contains the data to creates a new blob container.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Blobs.BlobContainerClient}"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse<BlobContainerClient> CreateBlobContainer<TIn>(
            TIn blobContainerMetadata,
            CancellationToken cancellationToken = default)
            where TIn : CreateBlobContainerMetadata
        {
            ThrowIfInvalidBlobContainerMetadata(blobContainerMetadata);

            return CreateBlobContainer(
                blobContainerMetadata.BlobContainerName,
                blobContainerMetadata.PublicAccessType,
                blobContainerMetadata.Metadata,
                cancellationToken);
        }

        /// <summary>
        /// Creates a new blob container under the specified account. If a container
        /// with the same name already exists, the operation fails.
        /// </summary>
        /// <param name="blobContainerName">The name of the container to create.</param>
        /// <param name="publicAccessType">Specifies whether data in the container may be accessed publicly and
        /// the level of access. Azure.Storage.Blobs.Models.PublicAccessType.BlobContainer
        /// specifies full public read access for container and blob data. Clients can enumerate
        /// blobs within the container via anonymous request, but cannot enumerate containers
        /// within the storage account. Azure.Storage.Blobs.Models.PublicAccessType.Blob
        /// specifies public read access for blobs. Blob data within this container can be
        /// read via anonymous request, but container data is not available. Clients cannot
        /// enumerate blobs within the container via anonymous request. Azure.Storage.Blobs.Models.PublicAccessType.None
        /// specifies that the container data is private to the account owner.</param>
        /// <param name="metadata">Custom metadata to set for this container.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Blobs.BlobContainerClient}"/> indicating the result of the operation.
        /// If the operation was successful the response contains an instance representing the created container.</returns>
        public virtual AzStorageResponse<BlobContainerClient> CreateBlobContainer(
            string blobContainerName,
            PublicAccessType publicAccessType = PublicAccessType.None,
            IDictionary<string, string> metadata = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidBlobContainerName(blobContainerName);
            ThrowIfInvalidBlobServiceClient();

            return FuncHelper.Execute<string, PublicAccessType, IDictionary<string, string>, CancellationToken, Response<BlobContainerClient>, AzStorageResponse<BlobContainerClient>, BlobContainerClient>(
                GetBlobServiceClient().CreateBlobContainer,
                blobContainerName, publicAccessType, metadata, cancellationToken);
        }

        /// <summary>
        /// Creates a new blob containers under the specified account. If a container
        /// with the same name of the property BlobContainerName of any entity in <paramref name="blobContainersMetadata"/> 
        /// already exists, the creation of the container with that name will fail.
        /// </summary>
        /// <param name="blobContainerNames">Contains the name of new blob containers.</param>
        /// <param name="publicAccessType">Specifies whether data in the all new containers may be accessed publicly and
        /// the level of access. Azure.Storage.Blobs.Models.PublicAccessType.BlobContainer
        /// specifies full public read access for container and blob data. Clients can enumerate
        /// blobs within the container via anonymous request, but cannot enumerate containers
        /// within the storage account. Azure.Storage.Blobs.Models.PublicAccessType.Blob
        /// specifies public read access for blobs. Blob data within this container can be
        /// read via anonymous request, but container data is not available. Clients cannot
        /// enumerate blobs within the container via anonymous request. Azure.Storage.Blobs.Models.PublicAccessType.None
        /// specifies that the container data is private to the account owner.</param>
        /// <param name="metadata">Custom metadata to set for all new containers.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{Azure.Storage.Blobs.BlobContainerClient}"/> 
        /// indicating the result of each create operation.</returns>
        public virtual List<AzStorageResponse<BlobContainerClient>> CreateBlobContainers(
            IEnumerable<string> blobContainerNames,
            PublicAccessType publicAccessType = PublicAccessType.None,
            IDictionary<string, string> metadata = null,
            CancellationToken cancellationToken = default)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(nameof(blobContainerNames), nameof(blobContainerNames));

            var results = GetAzStorageResponseList<BlobContainerClient>();
            AzStorageResponse<BlobContainerClient> _response;
            foreach (var _blobContainerName in blobContainerNames)
            {
                if (IsCancellationRequested(cancellationToken))
                    results.Add(GetAzStorageResponseWithOperationCanceledMessage<BlobContainerClient>());
                else
                {
                    try
                    {
                        _response = CreateBlobContainer(_blobContainerName, publicAccessType, metadata, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        _response = GetAzStorageResponseWithException<BlobContainerClient, Exception>(e);
                        if (ValidateBlobContainerName(_blobContainerName))
                            _response.Message += $" Blob container name:{_blobContainerName}.";
                    }

                    results.Add(_response);
                }
            }

            return results;
        }

        /// <summary>
        /// Creates a new blob containers under the specified account. If a container
        /// with the same name of the property BlobContainerName of any entity in <paramref name="blobContainersMetadata"/> 
        /// already exists, the creation of the container with that name will fail.
        /// </summary>
        /// <typeparam name="TIn">A custom model type.</typeparam>
        /// <param name="blobContainersMetadata">Contains the data to creates a new blob containers.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{Azure.Storage.Blobs.BlobContainerClient}"/> 
        /// indicating the result of each create operation.</returns>
        public virtual List<AzStorageResponse<BlobContainerClient>> CreateBlobContainers<TIn>(
            IEnumerable<TIn> blobContainersMetadata,
            CancellationToken cancellationToken = default)
            where TIn : CreateBlobContainerMetadata
        {
            ExThrower.ST_ThrowIfArgumentIsNull(nameof(blobContainersMetadata), nameof(blobContainersMetadata));

            var results = GetAzStorageResponseList<BlobContainerClient>();
            AzStorageResponse<BlobContainerClient> _response;
            foreach (var _blobContainerMetadata in blobContainersMetadata)
            {
                if (IsCancellationRequested(cancellationToken))
                    results.Add(GetAzStorageResponseWithOperationCanceledMessage<BlobContainerClient>());
                else
                {
                    try
                    {
                        _response = CreateBlobContainer(_blobContainerMetadata, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        _response = GetAzStorageResponseWithException<BlobContainerClient, Exception>(e);
                        if (ValidateBlobContainerMetadata(_blobContainerMetadata))
                            _response.Message += $" Blob container name:{_blobContainerMetadata.BlobContainerName}.";
                    }

                    results.Add(_response);
                }
            }

            return results;
        }

        /// <summary>
        /// Returns an async sequence of blob containers in the storage account.
        /// Enumerating the blob containers may make multiple requests to the service while
        /// fetching all the values. Containers are ordered lexicographically by name.
        /// </summary>
        /// <param name="traits">Specifies trait options for shaping the blob containers.</param>
        /// <param name="states">Specifies states options for shaping the blob containers.</param>
        /// <param name="prefix">Specifies a string that filters the results to return only containers whose name
        /// begins with the specified prefix.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of items to take.</param>
        /// <returns>A <see cref="AzStorageResponse{List{BlobContainerItem}}"/> containing the resulting collection of containers.</returns>
        public virtual AzStorageResponse<List<BlobContainerItem>> GetBlobContainers(
            BlobContainerTraits traits = BlobContainerTraits.None,
            BlobContainerStates states = BlobContainerStates.None,
            string prefix = null,
            CancellationToken cancellationToken = default,
            int take = ConstProvider.DefaultTake)
        {
            ThrowIfInvalidBlobServiceClient();

            return FuncHelper.Execute<BlobContainerTraits, BlobContainerStates, string, CancellationToken, int, AzStorageResponse<List<BlobContainerItem>>, AzStorageResponse<List<BlobContainerItem>>, List<BlobContainerItem>>(
                GetBlobServiceClient().GetBlobContainers,
                traits, states, prefix, cancellationToken, take);
        }

        /// <summary>
        /// Returns an async sequence of all or the amount to <paramref name="take"/> of blob containers 
        /// in the storage account service.
        /// Enumerating the blob containers may make multiple requests to the service while
        /// fetching all the values. Containers are ordered lexicographically by name.
        /// </summary>
        /// <param name="traits">Specifies trait options for shaping the blob containers.</param>
        /// <param name="states">Specifies states options for shaping the blob containers.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of items to take.</param>
        /// <returns>A <see cref="AzStorageResponse{List{BlobContainerItem}}"/> containing the resulting collection of containers, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual AzStorageResponse<List<BlobContainerItem>> GetAllBlobContainers(
            int take,
            BlobContainerTraits traits = BlobContainerTraits.None,
            BlobContainerStates states = BlobContainerStates.None,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidBlobServiceClient();

            return FuncHelper.Execute<int, BlobContainerTraits, BlobContainerStates, CancellationToken, AzStorageResponse<List<BlobContainerItem>>, AzStorageResponse<List<BlobContainerItem>>, List<BlobContainerItem>>(
                GetBlobServiceClient().GetAllBlobContainers,
                take, traits, states, cancellationToken);
        }

        /// <summary>
        /// Returns an async sequence of all or the Int.MaxValue amount of blob containers in the storage account service.
        /// Enumerating the blob containers may make multiple requests to the service while
        /// fetching all the values. Containers are ordered lexicographically by name.
        /// </summary>
        /// <param name="traits">Specifies trait options for shaping the blob containers.</param>
        /// <param name="states">Specifies states options for shaping the blob containers.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="AzStorageResponse{List{BlobContainerItem}}"/> containing the resulting collection of containers.</returns>
        public virtual AzStorageResponse<List<BlobContainerItem>> GetAllBlobContainers(
            BlobContainerTraits traits = BlobContainerTraits.None,
            BlobContainerStates states = BlobContainerStates.None,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidBlobServiceClient();

            return FuncHelper.Execute<BlobContainerTraits, BlobContainerStates, CancellationToken, AzStorageResponse<List<BlobContainerItem>>, AzStorageResponse<List<BlobContainerItem>>, List<BlobContainerItem>>(
                GetBlobServiceClient().GetAllBlobContainers,
                traits, states, cancellationToken);
        }

        /// <summary>
        /// Marks the specified blob container for deletion. The container and
        /// any blobs contained within it are later deleted during garbage collection.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="blobContainerMetadata">Contains the data to deletes a blob containers.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse DeleteBlobContainer<T>(
            T blobContainerMetadata,
            CancellationToken cancellationToken = default)
            where T : DeleteBlobContainerMetadata
        {
            ThrowIfInvalidBlobContainerMetadata(blobContainerMetadata);

            return DeleteBlobContainer(
                blobContainerMetadata.BlobContainerName,
                blobContainerMetadata.Conditions,
                cancellationToken);
        }

        /// <summary>
        /// Marks the specified blob container for deletion. The container and
        /// any blobs contained within it are later deleted during garbage collection.
        /// </summary>
        /// <param name="blobContainerName">The name of the container to delete.</param>
        /// <param name="conditions">Conditions to be added on deletion of the container.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse DeleteBlobContainer(
            string blobContainerName,
            BlobRequestConditions conditions = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidBlobContainerName(blobContainerName);
            ThrowIfInvalidBlobServiceClient();

            return FuncHelper.Execute<string, BlobRequestConditions, CancellationToken, Response, AzStorageResponse>(
                GetBlobServiceClient().DeleteBlobContainer,
                blobContainerName, conditions, cancellationToken);
        }

        /// <summary>
        /// Marks the specified blob containers for deletion. The containers and
        /// any blobs contained within it are later deleted during garbage collection.
        /// </summary>
        /// <param name="blobContainerNames">The name of the containers to delete.</param>
        /// <param name="conditions">Conditions to be added on deletion of the all containers.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{string}"/> indicating the result 
        /// of the operation and each response contains the name of container relative to its operation.</returns>
        public virtual List<AzStorageResponse<string>> DeleteBlobContainers(
            IEnumerable<string> blobContainerNames,
            BlobRequestConditions conditions = null,
            CancellationToken cancellationToken = default)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(nameof(blobContainerNames), nameof(blobContainerNames));

            var results = GetAzStorageResponseList<string>();
            AzStorageResponse _response;
            foreach (var _blobContainerName in blobContainerNames)
            {
                if (IsCancellationRequested(cancellationToken))
                    results.Add(GetAzStorageResponseWithOperationCanceledMessage<string>());
                else
                {
                    try
                    {
                        _response = DeleteBlobContainer(_blobContainerName, conditions, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        _response = GetAzStorageResponseWithException(e);

                        if (ValidateBlobContainerName(_blobContainerName))
                            _response.Message += $" Blob container name:{_blobContainerName}.";
                    }

                    results.Add(_response.InduceResponse(_blobContainerName));
                }
            }

            return results;
        }

        /// <summary>
        /// Marks the specified blob containers for deletion. The containers and
        /// any blobs contained within it are later deleted during garbage collection.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="blobContainersMetadata">Contains the data to deletes a blob containers.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{T}"/> indicating the result 
        /// of the operation and each response contains the <typeparamref name="T"/> entity relative to its operation.</returns>
        public virtual List<AzStorageResponse<T>> DeleteBlobContainers<T>(
            IEnumerable<T> blobContainersMetadata,
            CancellationToken cancellationToken = default)
            where T : DeleteBlobContainerMetadata
        {
            ExThrower.ST_ThrowIfArgumentIsNull(nameof(blobContainersMetadata), nameof(blobContainersMetadata));

            var results = GetAzStorageResponseList<T>();
            AzStorageResponse _response;
            foreach (var _blobContainerMetadata in blobContainersMetadata)
            {
                if (IsCancellationRequested(cancellationToken))
                    results.Add(GetAzStorageResponseWithOperationCanceledMessage<T>());
                else
                {
                    try
                    {
                        _response = DeleteBlobContainer(_blobContainerMetadata, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        _response = GetAzStorageResponseWithException(e);

                        if (ValidateBlobContainerMetadata(_blobContainerMetadata))
                            _response.Message += $" Blob container name:{_blobContainerMetadata.BlobContainerName}.";
                    }

                    if (ValidateBlobContainerMetadata(_blobContainerMetadata))
                        results.Add(_response.InduceResponse(_blobContainerMetadata));
                    else
                        results.Add(_response.InduceResponse<T>(default));
                }
            }
            
            return results;
        }

        /// <summary>
        /// Retrieves the containers and marks them for deletion. The containers and
        /// any blobs contained within it are later deleted during garbage collection.
        /// </summary>
        /// <param name="traits">Specifies trait options for shaping the blob containers.</param>
        /// <param name="states">Specifies states options for shaping the blob containers.</param>
        /// <param name="conditions">Conditions to be added on deletion of the all containers.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{string}"/> indicating the result 
        /// of the operation and each response contains the name of container relative to its operation.</returns>
        public virtual List<AzStorageResponse<string>> DeleteAllBlobContainers(
            BlobContainerTraits traits = BlobContainerTraits.None,
            BlobContainerStates states = BlobContainerStates.None,
            BlobRequestConditions conditions = null,
            CancellationToken cancellationToken = default)
        {
            var _getAllBlobContainersResponse = GetAllBlobContainers(traits, states, cancellationToken);
            if (!_getAllBlobContainersResponse.Succeeded)
            {
                var _failedResponse = _getAllBlobContainersResponse.InduceResponse<AzStorageResponse<string>>();
                _failedResponse.Message = $"{ErrorTextProvider.Not_possible_retrieve_containers_to_delete} {_failedResponse.Message}";
                return new List<AzStorageResponse<string>> { _failedResponse };
            }

            if (IsCancellationRequested(cancellationToken))
                return new List<AzStorageResponse<string>> { GetAzStorageResponseWithOperationCanceledMessage<string>() };

            return DeleteBlobContainers(
                _getAllBlobContainersResponse.Value.Select(_BCItem => _BCItem.Name),
                conditions,
                cancellationToken);
        }

        #endregion

        #region Container async

        /// <summary>
        /// Creates a new blob container under the specified account. If a container
        /// with the same name of the property BlobContainerName of <paramref name="blobContainerMetadata"/> already exists, 
        /// the operation fails.
        /// </summary>
        /// <typeparam name="TIn">A custom model type.</typeparam>
        /// <param name="blobContainerMetadata">Contains the data to creates a new blob container.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Blobs.BlobContainerClient}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.
        /// If the operation was successful the response contains an instance representing the created container.</returns>
        public virtual async Task<AzStorageResponse<BlobContainerClient>> CreateBlobContainerAsync<TIn>(
            TIn blobContainerMetadata,
            CancellationToken cancellationToken = default)
            where TIn : CreateBlobContainerMetadata
        {
            ThrowIfInvalidBlobContainerMetadata(blobContainerMetadata);

            return await CreateBlobContainerAsync(
                blobContainerMetadata.BlobContainerName,
                blobContainerMetadata.PublicAccessType,
                blobContainerMetadata.Metadata,
                cancellationToken);
        }

        /// <summary>
        /// Creates a new blob container under the specified account. If a container
        /// with the same name already exists, the operation fails.
        /// </summary>
        /// <param name="blobContainerName">The name of the container to create.</param>
        /// <param name="publicAccessType">Specifies whether data in the container may be accessed publicly and
        /// the level of access. Azure.Storage.Blobs.Models.PublicAccessType.BlobContainer
        /// specifies full public read access for container and blob data. Clients can enumerate
        /// blobs within the container via anonymous request, but cannot enumerate containers
        /// within the storage account. Azure.Storage.Blobs.Models.PublicAccessType.Blob
        /// specifies public read access for blobs. Blob data within this container can be
        /// read via anonymous request, but container data is not available. Clients cannot
        /// enumerate blobs within the container via anonymous request. Azure.Storage.Blobs.Models.PublicAccessType.None
        /// specifies that the container data is private to the account owner.</param>
        /// <param name="metadata">Custom metadata to set for this container.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Blobs.BlobContainerClient}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.
        /// If the operation was successful the response contains an instance representing the created container.</returns>
        public virtual async Task<AzStorageResponse<BlobContainerClient>> CreateBlobContainerAsync(
            string blobContainerName,
            PublicAccessType publicAccessType = PublicAccessType.None,
            IDictionary<string, string> metadata = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidBlobContainerName(blobContainerName);
            ThrowIfInvalidBlobServiceClient();

            return await FuncHelper.ExecuteAsync<string, PublicAccessType, IDictionary<string, string>, CancellationToken, Response<BlobContainerClient>, AzStorageResponse<BlobContainerClient>, BlobContainerClient>(
                GetBlobServiceClient().CreateBlobContainerAsync,
                blobContainerName, publicAccessType, metadata, cancellationToken);
        }

        /// <summary>
        /// Creates a new blob containers under the specified account. If a container
        /// with the same name of the property BlobContainerName of any entity in <paramref name="blobContainersMetadata"/> 
        /// already exists, the creation of the container with that name will fail.
        /// </summary>
        /// <param name="blobContainerNames">Contains the name of new blob containers.</param>
        /// <param name="publicAccessType">Specifies whether data in the all new containers may be accessed publicly and
        /// the level of access. Azure.Storage.Blobs.Models.PublicAccessType.BlobContainer
        /// specifies full public read access for container and blob data. Clients can enumerate
        /// blobs within the container via anonymous request, but cannot enumerate containers
        /// within the storage account. Azure.Storage.Blobs.Models.PublicAccessType.Blob
        /// specifies public read access for blobs. Blob data within this container can be
        /// read via anonymous request, but container data is not available. Clients cannot
        /// enumerate blobs within the container via anonymous request. Azure.Storage.Blobs.Models.PublicAccessType.None
        /// specifies that the container data is private to the account owner.</param>
        /// <param name="metadata">Custom metadata to set for all new containers.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{Azure.Storage.Blobs.BlobContainerClient}"/> 
        /// indicating the result of each create operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<List<AzStorageResponse<BlobContainerClient>>> CreateBlobContainersAsync(
            IEnumerable<string> blobContainerNames,
            PublicAccessType publicAccessType = PublicAccessType.None,
            IDictionary<string, string> metadata = null,
            CancellationToken cancellationToken = default)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(nameof(blobContainerNames), nameof(blobContainerNames));

            var results = GetAzStorageResponseList<BlobContainerClient>();
            AzStorageResponse<BlobContainerClient> _response;
            foreach (var _blobContainerName in blobContainerNames)
            {
                if (IsCancellationRequested(cancellationToken))
                    results.Add(GetAzStorageResponseWithOperationCanceledMessage<BlobContainerClient>());
                else
                {
                    try
                    {
                        _response = await CreateBlobContainerAsync(_blobContainerName, publicAccessType, metadata, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        _response = GetAzStorageResponseWithException<BlobContainerClient, Exception>(e);

                        if (ValidateBlobContainerName(_blobContainerName))
                            _response.Message += $" {AzStorageConstProvider.Blob_container_name}:{_blobContainerName}.";
                        else
                            _response.Message += $" {AzStorageConstProvider.Blob_container_name} is invalid.";
                    }

                    results.Add(_response);
                }
            }

            return results;
        }

        /// <summary>
        /// Creates a new blob containers under the specified account. If a container
        /// with the same name of the property BlobContainerName of any entity in <paramref name="blobContainersMetadata"/> 
        /// already exists, the creation of the container with that name will fail.
        /// </summary>
        /// <typeparam name="TIn">A custom model type.</typeparam>
        /// <param name="blobContainersMetadata">Contains the data to creates a new blob containers.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{Azure.Storage.Blobs.BlobContainerClient}"/> 
        /// indicating the result of each create operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<List<AzStorageResponse<BlobContainerClient>>> CreateBlobContainersAsync<TIn>(
            IEnumerable<TIn> blobContainersMetadata,
            CancellationToken cancellationToken = default)
            where TIn : CreateBlobContainerMetadata
        {
            ExThrower.ST_ThrowIfArgumentIsNull(nameof(blobContainersMetadata), nameof(blobContainersMetadata));

            var results = GetAzStorageResponseList<BlobContainerClient>();
            AzStorageResponse<BlobContainerClient> _response;
            foreach (var _blobContainerMetadata in blobContainersMetadata)
            {
                if (IsCancellationRequested(cancellationToken))
                    results.Add(GetAzStorageResponseWithOperationCanceledMessage<BlobContainerClient>());
                else
                {
                    try
                    {
                        _response = await CreateBlobContainerAsync(_blobContainerMetadata, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        _response = GetAzStorageResponseWithException<BlobContainerClient, Exception>(e);

                        if (ValidateBlobContainerMetadata(_blobContainerMetadata))
                            _response.Message += $" Blob container name:{_blobContainerMetadata.BlobContainerName}.";
                    }

                    results.Add(_response);
                }
            }

            return results;
        }

        /// <summary>
        /// Returns an async sequence of blob containers in the storage account.
        /// Enumerating the blob containers may make multiple requests to the service while
        /// fetching all the values. Containers are ordered lexicographically by name.
        /// </summary>
        /// <param name="traits">Specifies trait options for shaping the blob containers.</param>
        /// <param name="states">Specifies states options for shaping the blob containers.</param>
        /// <param name="prefix">Specifies a string that filters the results to return only containers whose name
        /// begins with the specified prefix.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of items to take.</param>
        /// <returns>A <see cref="AzStorageResponse{List{BlobContainerItem}}"/> containing the resulting collection of containers, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<List<BlobContainerItem>>> GetBlobContainersAsync(
            BlobContainerTraits traits = BlobContainerTraits.None,
            BlobContainerStates states = BlobContainerStates.None,
            string prefix = null,
            CancellationToken cancellationToken = default,
            int take = ConstProvider.DefaultTake)
        {
            ThrowIfInvalidBlobServiceClient();

            return await FuncHelper.ExecuteAsync<BlobContainerTraits, BlobContainerStates, string, CancellationToken, int, AzStorageResponse<List<BlobContainerItem>>, AzStorageResponse<List<BlobContainerItem>>, List<BlobContainerItem>>(
                GetBlobServiceClient().GetBlobContainersAsync,
                traits, states, prefix, cancellationToken, take);
        }

        /// <summary>
        /// Returns an async sequence of all or the amount to <paramref name="take"/> of blob containers 
        /// in the storage account service.
        /// Enumerating the blob containers may make multiple requests to the service while
        /// fetching all the values. Containers are ordered lexicographically by name.
        /// </summary>
        /// <param name="traits">Specifies trait options for shaping the blob containers.</param>
        /// <param name="states">Specifies states options for shaping the blob containers.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of items to take.</param>
        /// <returns>A <see cref="AzStorageResponse{List{BlobContainerItem}}"/> containing the resulting collection of containers, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<List<BlobContainerItem>>> GetAllBlobContainersAsync(
            int take,
            BlobContainerTraits traits = BlobContainerTraits.None,
            BlobContainerStates states = BlobContainerStates.None,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidBlobServiceClient();

            return await FuncHelper.ExecuteAsync<int, BlobContainerTraits, BlobContainerStates, CancellationToken, AzStorageResponse<List<BlobContainerItem>>, AzStorageResponse<List<BlobContainerItem>>, List<BlobContainerItem>>(
                GetBlobServiceClient().GetAllBlobContainersAsync,
                take, traits, states, cancellationToken);
        }

        /// <summary>
        /// Returns an async sequence of all or the Int.MaxValue amount of blob containers in the storage account service.
        /// Enumerating the blob containers may make multiple requests to the service while
        /// fetching all the values. Containers are ordered lexicographically by name.
        /// </summary>
        /// <param name="traits">Specifies trait options for shaping the blob containers.</param>
        /// <param name="states">Specifies states options for shaping the blob containers.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="AzStorageResponse{List{BlobContainerItem}}"/> containing the resulting collection of containers, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<List<BlobContainerItem>>> GetAllBlobContainersAsync(
            BlobContainerTraits traits = BlobContainerTraits.None,
            BlobContainerStates states = BlobContainerStates.None,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidBlobServiceClient();

            return await FuncHelper.ExecuteAsync<BlobContainerTraits, BlobContainerStates, CancellationToken, AzStorageResponse<List<BlobContainerItem>>, AzStorageResponse<List<BlobContainerItem>>, List<BlobContainerItem>>(
                GetBlobServiceClient().GetAllBlobContainersAsync,
                traits, states, cancellationToken);
        }

        /// <summary>
        /// Marks the specified blob container for deletion. The container and
        /// any blobs contained within it are later deleted during garbage collection.
        /// </summary>
        /// <param name="blobContainerName">The name of the container to delete.</param>
        /// <param name="conditions">Conditions to be added on deletion of the container.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse> DeleteBlobContainerAsync(
            string blobContainerName,
            BlobRequestConditions conditions = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfInvalidBlobContainerName(blobContainerName);
            ThrowIfInvalidBlobServiceClient();

            return await FuncHelper.ExecuteAsync<string, BlobRequestConditions, CancellationToken, Response, AzStorageResponse>(
                GetBlobServiceClient().DeleteBlobContainerAsync,
                blobContainerName, conditions, cancellationToken);
        }

        /// <summary>
        /// Marks the specified blob container for deletion. The container and
        /// any blobs contained within it are later deleted during garbage collection.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="blobContainerMetadata">Contains the data to deletes a blob containers.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse> DeleteBlobContainerAsync<T>(
            T blobContainerMetadata,
            CancellationToken cancellationToken = default)
            where T : DeleteBlobContainerMetadata
        {
            ThrowIfInvalidBlobContainerMetadata(blobContainerMetadata);

            return await DeleteBlobContainerAsync(
                blobContainerMetadata.BlobContainerName,
                blobContainerMetadata.Conditions,
                cancellationToken);
        }

        /// <summary>
        /// Marks the specified blob containers for deletion. The containers and
        /// any blobs contained within it are later deleted during garbage collection.
        /// </summary>
        /// <param name="blobContainerNames">The name of the containers to delete.</param>
        /// <param name="conditions">Conditions to be added on deletion of the all containers.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{string}"/> indicating the result 
        /// of the operation and each response contains the name of container relative to its operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<List<AzStorageResponse<string>>> DeleteBlobContainersAsync(
            IEnumerable<string> blobContainerNames,
            BlobRequestConditions conditions = null,
            CancellationToken cancellationToken = default)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(nameof(blobContainerNames), nameof(blobContainerNames));

            var results = GetAzStorageResponseList<string>();
            AzStorageResponse _response;
            foreach (var _blobContainerName in blobContainerNames)
            {
                if (IsCancellationRequested(cancellationToken))
                    results.Add(GetAzStorageResponseWithOperationCanceledMessage<string>());
                else
                {
                    try
                    {
                        _response = await DeleteBlobContainerAsync(_blobContainerName, conditions, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        _response = GetAzStorageResponseWithException(e);

                        if (ValidateBlobContainerName(_blobContainerName))
                            _response.Message += $" Blob container name:{_blobContainerName}.";
                    }

                    results.Add(_response.InduceResponse(_blobContainerName));
                }
            }

            return results;
        }

        /// <summary>
        /// Marks the specified blob containers for deletion. The containers and
        /// any blobs contained within it are later deleted during garbage collection.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="blobContainersMetadata">Contains the data to deletes a blob containers.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{T}"/> indicating the result of 
        /// the operation and each response contains the <typeparamref name="T"/> entity relative to its operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<List<AzStorageResponse<T>>> DeleteBlobContainersAsync<T>(
            IEnumerable<T> blobContainersMetadata,
            CancellationToken cancellationToken = default)
            where T : DeleteBlobContainerMetadata
        {
            ExThrower.ST_ThrowIfArgumentIsNull(nameof(blobContainersMetadata), nameof(blobContainersMetadata));

            var results = GetAzStorageResponseList<T>();
            AzStorageResponse _response;
            foreach (var _blobContainerMetadata in blobContainersMetadata)
            {
                if (IsCancellationRequested(cancellationToken))
                    results.Add(GetAzStorageResponseWithOperationCanceledMessage<T>());
                else
                {
                    try
                    {
                        _response = await DeleteBlobContainerAsync(_blobContainerMetadata, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        _response = GetAzStorageResponseWithException(e);

                        if (ValidateBlobContainerMetadata(_blobContainerMetadata))
                            _response.Message += $" Blob container name:{_blobContainerMetadata.BlobContainerName}.";
                    }

                    if (ValidateBlobContainerMetadata(_blobContainerMetadata))
                        results.Add(_response.InduceResponse(_blobContainerMetadata));
                    else
                        results.Add(_response.InduceResponse<T>(default));
                }
            }

            return results;
        }

        /// <summary>
        /// Retrieves the containers and marks them for deletion. The containers and
        /// any blobs contained within it are later deleted during garbage collection.
        /// </summary>
        /// <param name="traits">Specifies trait options for shaping the blob containers.</param>
        /// <param name="states">Specifies states options for shaping the blob containers.</param>
        /// <param name="conditions">Conditions to be added on deletion of the all containers.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{string}"/> indicating the result 
        /// of the operation and each response contains the name of container relative to its operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<List<AzStorageResponse<string>>> DeleteAllBlobContainersAsync(
            BlobContainerTraits traits = BlobContainerTraits.None,
            BlobContainerStates states = BlobContainerStates.None,
            BlobRequestConditions conditions = null,
            CancellationToken cancellationToken = default)
        {
            var _getAllBlobContainersResponse = await GetAllBlobContainersAsync(traits, states, cancellationToken);
            if (!_getAllBlobContainersResponse.Succeeded)
            {
                var _failedResponse = _getAllBlobContainersResponse.InduceResponse<AzStorageResponse<string>>();
                _failedResponse.Message = $"{ErrorTextProvider.Not_possible_retrieve_containers_to_delete} {_failedResponse.Message}";
                return new List<AzStorageResponse<string>> { _failedResponse };
            }

            if (IsCancellationRequested(cancellationToken))
                return new List<AzStorageResponse<string>> { GetAzStorageResponseWithOperationCanceledMessage<string>() };

            return await DeleteBlobContainersAsync(
                _getAllBlobContainersResponse.Value.Select(_BCItem => _BCItem.Name),
                conditions, 
                cancellationToken);
        }

        #endregion

        #region Upload new blob

        /// <summary>
        /// Creates a new block blob and upload the content.
        /// </summary>
        /// <param name="content">A System.IO.Stream containing the content to upload.</param>
        /// <param name="overwrite">Whether the upload should overwrite any existing blobs. The default value is false.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="blobContainerName">The name of the container.</param>
        /// <param name="blobName">The name of the blob.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Blobs.Models.BlobContentInfo}"/> indicating the result of the operation.</returns>
        /// <exception cref="Azure.RequestFailedException">A <see cref="Azure.RequestFailedException"/> 
        /// will be thrown if a failure occurs.</exception>
        public virtual AzStorageResponse<BlobContentInfo> UploadBlob(
            Stream content, 
            bool overwrite = false, 
            CancellationToken cancellationToken = default,
            string blobContainerName = default,
            string blobName = default)
        {
            if (!TryInitialize(blobContainerName, blobName, false, false, out AzStorageResponse azStorageResponse))
                return azStorageResponse.InduceResponse<BlobContentInfo>();

            return FuncHelper.Execute<Stream, bool, CancellationToken, Response<BlobContentInfo>, AzStorageResponse<BlobContentInfo>, BlobContentInfo>(
                GetBlobClient().Upload, content, overwrite, cancellationToken);
        }

        /// <summary>
        /// Creates a new block blob and upload the content.
        /// </summary>
        /// <param name="content">A System.BinaryData containing the content to upload.</param>
        /// <param name="overwrite">Whether the upload should overwrite any existing blobs. The default value is false.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="blobContainerName">The name of the container.</param>
        /// <param name="blobName">The name of the blob.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Blobs.Models.BlobContentInfo}"/> indicating the result of the operation.</returns>
        /// <exception cref="Azure.RequestFailedException">A <see cref="Azure.RequestFailedException"/> 
        /// will be thrown if a failure occurs.</exception>
        public virtual AzStorageResponse<BlobContentInfo> UploadBlob(
            BinaryData content,
            bool overwrite = false,
            CancellationToken cancellationToken = default,
            string blobContainerName = default,
            string blobName = default)
        {
            if (!TryInitialize(blobContainerName, blobName, false, false, out AzStorageResponse azStorageResponse))
                return azStorageResponse.InduceResponse<BlobContentInfo>();

            return FuncHelper.Execute<BinaryData, bool, CancellationToken, Response<BlobContentInfo>, AzStorageResponse<BlobContentInfo>, BlobContentInfo>(
                GetBlobClient().Upload, content, overwrite, cancellationToken);
        }

        /// <summary>
        /// Creates a new block blob and upload the content in the file.
        /// </summary>
        /// <param name="path">A file path containing the content to upload.</param>
        /// <param name="overwrite">Whether the upload should overwrite any existing blobs. The default value is false.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="blobContainerName">The name of the container.</param>
        /// <param name="blobName">The name of the blob.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Blobs.Models.BlobContentInfo}"/> indicating the result of the operation.</returns>
        /// <exception cref="Azure.RequestFailedException">A <see cref="Azure.RequestFailedException"/> 
        /// will be thrown if a failure occurs.</exception>
        public virtual AzStorageResponse<BlobContentInfo> UploadBlob(
            string path,
            bool overwrite = false,
            CancellationToken cancellationToken = default,
            string blobContainerName = default,
            string blobName = default)
        {
            if (!TryInitialize(blobContainerName, blobName, false, false, out AzStorageResponse azStorageResponse))
                return azStorageResponse.InduceResponse<BlobContentInfo>();

            return FuncHelper.Execute<string, bool, CancellationToken, Response<BlobContentInfo>, AzStorageResponse<BlobContentInfo>, BlobContentInfo>(
                GetBlobClient().Upload, path, overwrite, cancellationToken);
        }

        /// <summary>
        /// Creates a new block blob and upload the content.
        /// </summary>
        /// <param name="content">A System.IO.Stream containing the content to upload.</param>
        /// <param name="options">Optional parameters.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="blobContainerName">The name of the container.</param>
        /// <param name="blobName">The name of the blob.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Blobs.Models.BlobContentInfo}"/> indicating the result of the operation.</returns>
        /// <exception cref="Azure.RequestFailedException">A <see cref="Azure.RequestFailedException"/> 
        /// will be thrown if a failure occurs.</exception>
        public virtual AzStorageResponse<BlobContentInfo> UploadBlob(
            Stream content,
            BlobUploadOptions options = null,
            CancellationToken cancellationToken = default,
            string blobContainerName = default,
            string blobName = default)
        {
            if (!TryInitialize(blobContainerName, blobName, false, false, out AzStorageResponse azStorageResponse))
                return azStorageResponse.InduceResponse<BlobContentInfo>();

            return FuncHelper.Execute<Stream, BlobUploadOptions, CancellationToken, Response<BlobContentInfo>, AzStorageResponse<BlobContentInfo>, BlobContentInfo>(
                GetBlobClient().Upload, content, options, cancellationToken);
        }

        /// <summary>
        /// Creates a new block blob and upload the content.
        /// </summary>
        /// <param name="content">A System.BinaryData containing the content to upload.</param>
        /// <param name="options">Optional parameters.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="blobContainerName">The name of the container.</param>
        /// <param name="blobName">The name of the blob.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Blobs.Models.BlobContentInfo}"/> indicating the result of the operation.</returns>
        /// <exception cref="Azure.RequestFailedException">A <see cref="Azure.RequestFailedException"/> 
        /// will be thrown if a failure occurs.</exception>
        public virtual AzStorageResponse<BlobContentInfo> UploadBlob(
            BinaryData content,
            BlobUploadOptions options = null,
            CancellationToken cancellationToken = default,
            string blobContainerName = default,
            string blobName = default)
        {
            if (!TryInitialize(blobContainerName, blobName, false, false, out AzStorageResponse azStorageResponse))
                return azStorageResponse.InduceResponse<BlobContentInfo>();

            return FuncHelper.Execute<BinaryData, BlobUploadOptions, CancellationToken, Response<BlobContentInfo>, AzStorageResponse<BlobContentInfo>, BlobContentInfo>(
                GetBlobClient().Upload, content, options, cancellationToken);
        }

        /// <summary>
        /// Creates a new block blob and upload the content in the file.
        /// </summary>
        /// <param name="path">A file path containing the content to upload.</param>
        /// <param name="options">Optional parameters.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="blobContainerName">The name of the container.</param>
        /// <param name="blobName">The name of the blob.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Blobs.Models.BlobContentInfo}"/> indicating the result of the operation.</returns>
        /// <exception cref="Azure.RequestFailedException">A <see cref="Azure.RequestFailedException"/> 
        /// will be thrown if a failure occurs.</exception>
        public virtual AzStorageResponse<BlobContentInfo> UploadBlob(
            string path,
            BlobUploadOptions options = null,
            CancellationToken cancellationToken = default,
            string blobContainerName = default,
            string blobName = default)
        {
            if (!TryInitialize(blobContainerName, blobName, false, false, out AzStorageResponse azStorageResponse))
                return azStorageResponse.InduceResponse<BlobContentInfo>();

            return FuncHelper.Execute<string, BlobUploadOptions, CancellationToken, Response<BlobContentInfo>, AzStorageResponse<BlobContentInfo>, BlobContentInfo>(
                GetBlobClient().Upload, path, options, cancellationToken);
        }

        #endregion

        #region Upload new blob async

        /// <summary>
        /// Creates a new block blob and upload the content.
        /// </summary>
        /// <param name="content">A System.IO.Stream containing the content to upload.</param>
        /// <param name="overwrite">Whether the upload should overwrite any existing blobs. The default value is false.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="blobContainerName">The name of the container.</param>
        /// <param name="blobName">The name of the blob.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Blobs.Models.BlobContentInfo}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        /// <exception cref="Azure.RequestFailedException">A <see cref="Azure.RequestFailedException"/> 
        /// will be thrown if a failure occurs.</exception>
        public virtual async Task<AzStorageResponse<BlobContentInfo>> UploadBlobAsync(
            Stream content,
            bool overwrite = false,
            CancellationToken cancellationToken = default,
            string blobContainerName = default,
            string blobName = default)
        {
            if (!TryInitialize(blobContainerName, blobName, false, false, out AzStorageResponse azStorageResponse))
                return azStorageResponse.InduceResponse<BlobContentInfo>();

            return await FuncHelper.ExecuteAsync<Stream, bool, CancellationToken, Response<BlobContentInfo>, AzStorageResponse<BlobContentInfo>, BlobContentInfo>(
                GetBlobClient().UploadAsync, content, overwrite, cancellationToken);
        }

        /// <summary>
        /// Creates a new block blob and upload the content.
        /// </summary>
        /// <param name="content">A byte array containing the content to upload.</param>
        /// <param name="overwrite">Whether the upload should overwrite any existing blobs. The default value is false.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="blobContainerName">The name of the container.</param>
        /// <param name="blobName">The name of the blob.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Blobs.Models.BlobContentInfo}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        /// <exception cref="Azure.RequestFailedException">A <see cref="Azure.RequestFailedException"/> 
        /// will be thrown if a failure occurs.</exception>
        public virtual async Task<AzStorageResponse<BlobContentInfo>> UploadBlobAsync(
            byte[] content,
            bool overwrite = false,
            CancellationToken cancellationToken = default,
            string blobContainerName = default,
            string blobName = default)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(content, nameof(content), nameof(content));

            BinaryData binDataContent;
            try
            {
                binDataContent = new BinaryData(content);
            }
            catch (Exception e)
            {
                return AzStorageResponse<BlobContentInfo>.Create(e);
            }

            return await UploadBlobAsync(binDataContent, overwrite, cancellationToken, blobContainerName, blobName);
        }
        
        /// <summary>
        /// Creates a new block blob and upload the content.
        /// </summary>
        /// <param name="content">A System.BinaryData containing the content to upload.</param>
        /// <param name="overwrite">Whether the upload should overwrite any existing blobs. The default value is false.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="blobContainerName">The name of the container.</param>
        /// <param name="blobName">The name of the blob.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Blobs.Models.BlobContentInfo}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        /// <exception cref="Azure.RequestFailedException">A <see cref="Azure.RequestFailedException"/> 
        /// will be thrown if a failure occurs.</exception>
        public virtual async Task<AzStorageResponse<BlobContentInfo>> UploadBlobAsync(
            BinaryData content,
            bool overwrite = false,
            CancellationToken cancellationToken = default,
            string blobContainerName = default,
            string blobName = default)
        {
            if (!TryInitialize(blobContainerName, blobName, false, false, out AzStorageResponse azStorageResponse))
                return azStorageResponse.InduceResponse<BlobContentInfo>();

            return await FuncHelper.ExecuteAsync<BinaryData, bool, CancellationToken, Response<BlobContentInfo>, AzStorageResponse<BlobContentInfo>, BlobContentInfo>(
                GetBlobClient().UploadAsync, content, overwrite, cancellationToken);
        }

        /// <summary>
        /// Creates a new block blob and upload the content in the file.
        /// </summary>
        /// <param name="path">A file path containing the content to upload.</param>
        /// <param name="overwrite">Whether the upload should overwrite any existing blobs. The default value is false.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="blobContainerName">The name of the container.</param>
        /// <param name="blobName">The name of the blob.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Blobs.Models.BlobContentInfo}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        /// <exception cref="Azure.RequestFailedException">A <see cref="Azure.RequestFailedException"/> 
        /// will be thrown if a failure occurs.</exception>
        public virtual async Task<AzStorageResponse<BlobContentInfo>> UploadBlobAsync(
            string path,
            bool overwrite = false,
            CancellationToken cancellationToken = default,
            string blobContainerName = default,
            string blobName = default)
        {
            if (!TryInitialize(blobContainerName, blobName, false, false, out AzStorageResponse azStorageResponse))
                return azStorageResponse.InduceResponse<BlobContentInfo>();

            return await FuncHelper.ExecuteAsync<string, bool, CancellationToken, Response<BlobContentInfo>, AzStorageResponse<BlobContentInfo>, BlobContentInfo>(
                GetBlobClient().UploadAsync, path, overwrite, cancellationToken);
        }

        /// <summary>
        /// Creates a new block blob and upload the content.
        /// </summary>
        /// <param name="content">A System.IO.Stream containing the content to upload.</param>
        /// <param name="options">Optional parameters.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="blobContainerName">The name of the container.</param>
        /// <param name="blobName">The name of the blob.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Blobs.Models.BlobContentInfo}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        /// <exception cref="Azure.RequestFailedException">A <see cref="Azure.RequestFailedException"/> 
        /// will be thrown if a failure occurs.</exception>
        public virtual async Task<AzStorageResponse<BlobContentInfo>> UploadBlobAsync(
            Stream content,
            BlobUploadOptions options = null,
            CancellationToken cancellationToken = default,
            string blobContainerName = default,
            string blobName = default)
        {
            if (!TryInitialize(blobContainerName, blobName, false, false, out AzStorageResponse azStorageResponse))
                return azStorageResponse.InduceResponse<BlobContentInfo>();

            return await FuncHelper.ExecuteAsync<Stream, BlobUploadOptions, CancellationToken, Response<BlobContentInfo>, AzStorageResponse<BlobContentInfo>, BlobContentInfo>(
                GetBlobClient().UploadAsync, content, options, cancellationToken);
        }

        /// <summary>
        /// Creates a new block blob and upload the content.
        /// </summary>
        /// <param name="content">A System.BinaryData containing the content to upload.</param>
        /// <param name="options">Optional parameters.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="blobContainerName">The name of the container.</param>
        /// <param name="blobName">The name of the blob.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Blobs.Models.BlobContentInfo}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        /// <exception cref="Azure.RequestFailedException">A <see cref="Azure.RequestFailedException"/> 
        /// will be thrown if a failure occurs.</exception>
        public virtual async Task<AzStorageResponse<BlobContentInfo>> UploadBlobAsync(
            BinaryData content,
            BlobUploadOptions options = null,
            CancellationToken cancellationToken = default,
            string blobContainerName = default,
            string blobName = default)
        {
            if (!TryInitialize(blobContainerName, blobName, false, false, out AzStorageResponse azStorageResponse))
                return azStorageResponse.InduceResponse<BlobContentInfo>();

            return await FuncHelper.ExecuteAsync<BinaryData, BlobUploadOptions, CancellationToken, Response<BlobContentInfo>, AzStorageResponse<BlobContentInfo>, BlobContentInfo>(
                GetBlobClient().UploadAsync, content, options, cancellationToken);
        }

        /// <summary>
        /// Creates a new block blob and upload the content in the file.
        /// </summary>
        /// <param name="path">A file path containing the content to upload.</param>
        /// <param name="options">Optional parameters.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="blobContainerName">The name of the container.</param>
        /// <param name="blobName">The name of the blob.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Blobs.Models.BlobContentInfo}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        /// <exception cref="Azure.RequestFailedException">A <see cref="Azure.RequestFailedException"/> 
        /// will be thrown if a failure occurs.</exception>
        public virtual async Task<AzStorageResponse<BlobContentInfo>> UploadBlobAsync(
            string path,
            BlobUploadOptions options = null,
            CancellationToken cancellationToken = default,
            string blobContainerName = default,
            string blobName = default)
        {
            if (!TryInitialize(blobContainerName, blobName, false, false, out AzStorageResponse azStorageResponse))
                return azStorageResponse.InduceResponse<BlobContentInfo>();

            return await FuncHelper.ExecuteAsync<string, BlobUploadOptions, CancellationToken, Response<BlobContentInfo>, AzStorageResponse<BlobContentInfo>, BlobContentInfo>(
                GetBlobClient().UploadAsync, path, options, cancellationToken);
        }

        #endregion

        #region DownloadContent async

        /// <summary>
        /// Downloads a blob from the service, including its metadata and properties.
        /// </summary>
        /// <param name="conditions">Azure.Storage.Blobs.Models.BlobRequestConditions to add conditions on
        /// downloading the blob.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="blobContainerName">The name of the container.</param>
        /// <param name="blobName">The name of the blob.</param>
        /// <returns>The <see cref="AzStorageResponse{Sysem.BinaryData}"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse<BinaryData> DownloadBlobBinaryData(
            BlobRequestConditions conditions = null,
            CancellationToken cancellationToken = default,
            string blobContainerName = default,
            string blobName = default)
        {
            var _blobDownloadResult = DownloadBlob(conditions, cancellationToken, blobContainerName, blobName);

            var response = _blobDownloadResult.InduceResponse(_blobDownloadResult.Value.Content);

            return response;
        }

        /// <summary>
        /// Downloads a blob from the service, including its metadata and properties.
        /// </summary>
        /// <param name="conditions">Azure.Storage.Blobs.Models.BlobRequestConditions to add conditions on
        /// downloading the blob.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="blobContainerName">The name of the container.</param>
        /// <param name="blobName">The name of the blob.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Blobs.Models.BlobDownloadResult}"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse<BlobDownloadResult> DownloadBlob(
            BlobRequestConditions conditions = null,
            CancellationToken cancellationToken = default,
            string blobContainerName = default,
            string blobName = default)
        {
            if (!TryInitialize(blobContainerName, blobName, false, false, out AzStorageResponse azStorageResponse))
                return azStorageResponse.InduceResponse<BlobDownloadResult>();

            return FuncHelper.Execute<BlobRequestConditions, CancellationToken, Response<BlobDownloadResult>, AzStorageResponse<BlobDownloadResult>, BlobDownloadResult>(
                GetBlobClient().DownloadContent, conditions, cancellationToken);
        }

        #endregion

        #region DownloadContent async

        /// <summary>
        /// Downloads a blob from the service, including its metadata and properties.
        /// </summary>
        /// <param name="conditions">Azure.Storage.Blobs.Models.BlobRequestConditions to add conditions on
        /// downloading the blob.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="blobContainerName">The name of the container.</param>
        /// <param name="blobName">The name of the blob.</param>
        /// <returns>The <see cref="AzStorageResponse{Sysem.BinaryData}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<BinaryData>> DownloadBlobBinaryDataAsync(
            BlobRequestConditions conditions = null,
            CancellationToken cancellationToken = default,
            string blobContainerName = default,
            string blobName = default)
        {
            var _blobDownloadResult = await DownloadBlobAsync(conditions, cancellationToken, blobContainerName, blobName);

            var response = _blobDownloadResult.InduceResponse(_blobDownloadResult.Value.Content);

            return response;
        }

        /// <summary>
        /// Downloads a blob from the service, including its metadata and properties.
        /// </summary>
        /// <param name="conditions">Azure.Storage.Blobs.Models.BlobRequestConditions to add conditions on
        /// downloading the blob.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="blobContainerName">The name of the container.</param>
        /// <param name="blobName">The name of the blob.</param>
        /// <returns>The <see cref="AzStorageResponse{Azure.Storage.Blobs.Models.BlobDownloadResult}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<BlobDownloadResult>> DownloadBlobAsync(
            BlobRequestConditions conditions = null,
            CancellationToken cancellationToken = default,
            string blobContainerName = default,
            string blobName = default)
        {
            if (!TryInitialize(blobContainerName, blobName, false, false, out AzStorageResponse azStorageResponse))
                return azStorageResponse.InduceResponse<BlobDownloadResult>();

            return await FuncHelper.ExecuteAsync<BlobRequestConditions, CancellationToken, Response<BlobDownloadResult>, AzStorageResponse<BlobDownloadResult>, BlobDownloadResult>(
                GetBlobClient().DownloadContentAsync, conditions, cancellationToken);
        }

        #endregion

        #region Delete blob

        /// <summary>
        /// Marks the specified blob or snapshot for deletion. The blob is later deleted during garbage collection. 
        /// Note that in order to delete a blob, you must delete all of its snapshots. 
        /// You can delete both at the same time using Azure.Storage.Blobs.Models.DeleteSnapshotsOption.IncludeSnapshots.
        /// </summary>
        /// <param name="snapshotsOption">Specifies options for deleting blob snapshots.</param>
        /// <param name="conditions">Optional Azure.Storage.Blobs.Models.BlobRequestConditions to add conditions on 
        /// deleting this blob.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="blobContainerName">The name of the container.</param>
        /// <param name="blobName">The name of the blob.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse DeleteBlob(
            DeleteSnapshotsOption snapshotsOption = DeleteSnapshotsOption.None,
            BlobRequestConditions conditions = null,
            CancellationToken cancellationToken = default,
            string blobContainerName = default,
            string blobName = default)
        {
            if (!TryInitialize(blobContainerName, blobName, false, false, out AzStorageResponse azStorageResponse))
                return azStorageResponse;

            return FuncHelper.Execute<DeleteSnapshotsOption, BlobRequestConditions, CancellationToken, Response, AzStorageResponse>(
                GetBlobClient().Delete, snapshotsOption, conditions, cancellationToken);
        }

        #endregion

        #region Delete blob async

        /// <summary>
        /// Marks the specified blob or snapshot for deletion. The blob is later deleted during garbage collection. 
        /// Note that in order to delete a blob, you must delete all of its snapshots. 
        /// You can delete both at the same time using Azure.Storage.Blobs.Models.DeleteSnapshotsOption.IncludeSnapshots.
        /// </summary>
        /// <param name="snapshotsOption">Specifies options for deleting blob snapshots.</param>
        /// <param name="conditions">Optional Azure.Storage.Blobs.Models.BlobRequestConditions to add conditions on 
        /// deleting this blob.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="blobContainerName">The name of the container.</param>
        /// <param name="blobName">The name of the blob.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse> DeleteBlobAsync(
            DeleteSnapshotsOption snapshotsOption = DeleteSnapshotsOption.None,
            BlobRequestConditions conditions = null,
            CancellationToken cancellationToken = default,
            string blobContainerName = default,
            string blobName = default)
        {
            if (!TryInitialize(blobContainerName, blobName, false, false, out AzStorageResponse azStorageResponse))
                return azStorageResponse;

            return await FuncHelper.ExecuteAsync<DeleteSnapshotsOption, BlobRequestConditions, CancellationToken, Response, AzStorageResponse>(
                GetBlobClient().DeleteAsync, snapshotsOption, conditions, cancellationToken);
        }

        #endregion
    }
}
