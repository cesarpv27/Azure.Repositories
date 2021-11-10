﻿using AzCoreTools.Core;
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

namespace AzStorage.Repositories
{
    public class AzBlobRepository : AzStorageRepository<AzBlobRetryOptions>
    {
        protected AzBlobRepository() { }

        public AzBlobRepository(string connectionString,
            string blobContainerName = default,
            string blobName = default,
            CreateResourcePolicy createTableResource = CreateResourcePolicy.OnlyFirstTime,
            AzBlobRetryOptions retryOptions = null) : base(createTableResource, retryOptions)
        {
            ThrowIfInvalidConnectionString(connectionString);

            ConnectionString = connectionString;
            BlobContainerName = blobContainerName;
            BlobName = blobName;

            InitializeForcingNotMandatory();
        }

        #region Properties

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
            if (!ValidateBlobServiceClient(BlobServiceClient))
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
            CreateOrLoadBlobServiceClient();
            Initialize(BlobContainerName, BlobName, false, false);
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

        protected virtual AzStorageResponse<BlobContainerClient> GetBlobContainerClient(string blobContainerName)
        {
            ThrowIfInvalidBlobContainerName(blobContainerName);
            ThrowIfInvalidBlobServiceClient();

            var _blobContainerClient = BlobServiceClient.GetBlobContainerClient(blobContainerName);
            return AzStorageResponse<BlobContainerClient>.Create(_blobContainerClient, _blobContainerClient != null);
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
            if (!ValidateBlobServiceClient(BlobServiceClient)
                || string.IsNullOrEmpty(BlobServiceClientAbsoluteUri)
                || !BlobServiceClientAbsoluteUri.Equals(BlobServiceClient.Uri?.AbsoluteUri))
            {
                BlobServiceClient = new BlobServiceClient(connectionString);
                BlobServiceClientAbsoluteUri = BlobServiceClient.Uri?.AbsoluteUri;
            }
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

            CreateOrLoadBlobContainerClient(blobContainerName, CreateBlobContainerClient);
        }

        private void CreateOrLoadBlobContainerClient(string blobContainerName, Func<dynamic[], BlobContainerClient> func)
        {
            if (!ValidateBlobContainerClient(BlobContainerClient) || string.IsNullOrEmpty(BlobContainerName)
                || !BlobContainerName.Equals(blobContainerName))
                IsFirstTimeBlobContainerClientCreation = true;

            bool _isFirstTime = IsFirstTimeBlobContainerClientCreation;

            BlobContainerClient _blobContainerClientResponse;
            var result = TryCreateResource(func, new dynamic[] { blobContainerName },
                ref _isFirstTime, out _blobContainerClientResponse);

            IsFirstTimeBlobContainerClientCreation = _isFirstTime;

            if (result && ValidateBlobContainerClient(_blobContainerClientResponse))
                BlobContainerClient = _blobContainerClientResponse;
        }

        private BlobContainerClient CreateBlobContainerClient(dynamic[] @params)
        {
            return CreateBlobContainerClient(@params[0]);
        }

        private BlobContainerClient CreateBlobContainerClient(string blobContainerName)
        {
            return BlobServiceClient.CreateBlobContainer(blobContainerName);
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
                BlobClient = _blobClientResponse;
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
                BlobServiceClient.CreateBlobContainer,
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
                BlobServiceClient.GetBlobContainers,
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
                BlobServiceClient.GetAllBlobContainers,
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
                BlobServiceClient.GetAllBlobContainers,
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
                BlobServiceClient.DeleteBlobContainer,
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
                BlobServiceClient.CreateBlobContainerAsync,
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
                BlobServiceClient.GetBlobContainersAsync,
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
                BlobServiceClient.GetAllBlobContainersAsync,
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
                BlobServiceClient.GetAllBlobContainersAsync,
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
                BlobServiceClient.DeleteBlobContainerAsync,
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
    }
}
