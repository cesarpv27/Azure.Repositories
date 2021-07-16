using AzStorage.Repositories.Core;
using System;
using System.Collections.Generic;
using System.Text;
using AzCoreTools.Core;
using ExThrower = CoreTools.Throws.ExceptionThrower;
using Microsoft.Azure.Cosmos;
using AzStorage.Core.Cosmos;
using System.Threading.Tasks;
using AzCoreTools.Core.Validators;
using CoreTools.Extensions;
using AzCoreTools.Helpers;
using System.Threading;
using AzStorage.Core.Utilities;

namespace AzStorage.Repositories
{
    public class AzCosmosDBRepository : AzRepository<AzCosmosRetryOptions>
    {
        protected AzCosmosDBRepository() : base() { }

        public AzCosmosDBRepository(string accountEndpointUri,
            string authKeyOrResourceToken,
            string databaseId,
            string containerId,
            string partitionKeyPropName,
            CreateResourcePolicy optionCreateTableResource = CreateResourcePolicy.OnlyFirstTime,
            AzCosmosRetryOptions retryOptions = null) : base(optionCreateTableResource, retryOptions)
        {
            Initialize(accountEndpointUri, authKeyOrResourceToken, databaseId, containerId, partitionKeyPropName);
        }

        #region Properties

        public virtual string EndpointUri { get; set; }
        public virtual string AuthKeyOrResourceToken { get; set; }

        public virtual string PartitionKeyPath { get; protected set; }

        public virtual CosmosClient CosmosClient { get; protected set; }
        public virtual Database Database { get; protected set; }
        public virtual Container Container { get; protected set; }

        protected virtual bool IsFirstTimeCosmosClientCreation { get; set; } = true;
        protected virtual bool IsFirstTimeDatabaseCreation { get; set; } = true;
        protected virtual bool IsFirstTimeContainerCreation { get; set; } = true;

        #endregion

        #region Throws

        protected virtual void ThrowIfInvalid_AccountEndpointUri_AuthKeyOrResourceToken(
            string accountEndpointUri,
            string authKeyOrResourceToken)
        {
            ThrowIfInvalidAccountEndpointUri(accountEndpointUri);
            ThrowIfInvalidAuthKeyOrResourceToken(authKeyOrResourceToken);
        }

        protected virtual void ThrowIfInvalidAccountEndpointUri(string accountEndpointUri)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(accountEndpointUri, nameof(accountEndpointUri));
        }

        protected virtual void ThrowIfInvalidAuthKeyOrResourceToken(string authKeyOrResourceToken)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(authKeyOrResourceToken, nameof(authKeyOrResourceToken));
        }

        protected virtual void ThrowIfInvalidDatabaseId(string databaseId)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(databaseId, nameof(databaseId));
        }

        protected virtual void ThrowIfInvalidContainerId(string containerId)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(containerId, nameof(containerId));
        }

        protected virtual void ThrowIfInvalidPartitionKeyPath(string partitionKeyPath)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(partitionKeyPath, nameof(partitionKeyPath), nameof(partitionKeyPath));
            if (!partitionKeyPath.StartsWith(ConstProvider.CosmosPartitionKeyPathStartPattern))
                ExThrower.ST_ThrowArgumentException(nameof(partitionKeyPath), $"{nameof(partitionKeyPath)} does not have the expected format");
        }

        protected virtual void ThrowIfInvalidCosmosClient()
        {
            ExThrower.ST_ThrowIfArgumentIsNull(CosmosClient, nameof(CosmosClient), nameof(CosmosClient));
        }

        protected virtual void ThrowIfInvalidDatabase()
        {
            ExThrower.ST_ThrowIfArgumentIsNull(Database, nameof(Database), nameof(Database));
        }

        //protected virtual void ThrowIfInvalidContainer()
        //{
        //    ExThrower.ST_ThrowIfArgumentIsNull(Container, nameof(Container), nameof(Container));
        //}

        //protected virtual void ThrowIfInvalidCreateContainerResponse(ContainerResponse response)
        //{
        //    if (!ResponseValidator.StatusSucceeded(response.StatusCode))
        //        ExThrower.ST_ThrowApplicationException($"Can not create container. StatusCode:{response.StatusCode}");
        //}

        protected virtual void ThrowIfInvalidEntity<T>(T entity) where T: BaseCosmosEntity
        {
            ExThrower.ST_ThrowIfArgumentIsNull(entity, nameof(entity));

            ThrowIfInvalidPartitionKeyValue(entity.PartitionKey);
            ThrowIfInvalidId(entity.Id);
        }

        protected virtual void ThrowIfInvalidPartitionKeyValue(string partitionKey)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(partitionKey, nameof(partitionKey));
        }

        protected virtual void ThrowIfInvalidId(string id)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(id, nameof(id));
        }

        #endregion

        #region Protected methods

        protected virtual void Initialize(string accountEndpointUri,
            string authKeyOrResourceToken,
            string databaseId,
            string containerId,
            string partitionKeyPath)
        {
            CreateOrLoadCosmosClient(accountEndpointUri, authKeyOrResourceToken);
            Initialize(databaseId, containerId, partitionKeyPath, true);
        }

        private void Initialize(string databaseId,
            string containerId,
            string partitionKeyPath, bool throwIfNull)
        {
            if (throwIfNull || !string.IsNullOrEmpty(databaseId))
                CreateOrLoadDatabase(databaseId);
            if (throwIfNull || (!string.IsNullOrEmpty(containerId) && !string.IsNullOrEmpty(partitionKeyPath)))
                CreateOrLoadContainer(containerId, partitionKeyPath);
        }

        #endregion

        #region CosmosClient creator

        /// <summary>
        /// Creates a new CosmosClient with the account endpoint URI string and account key.
        /// </summary>
        /// <param name="accountEndpointUri">The cosmos service endpoint to use.</param>
        /// <param name="authKeyOrResourceToken">The cosmos account key or resource token to use to create the client.</param>
        protected virtual void CreateOrLoadCosmosClient(string accountEndpointUri, string authKeyOrResourceToken)
        {
            ThrowIfInvalid_AccountEndpointUri_AuthKeyOrResourceToken(accountEndpointUri, authKeyOrResourceToken);

            CreateOrLoadCosmosClient(accountEndpointUri, authKeyOrResourceToken, CreateCosmosClient);
        }

        private void CreateOrLoadCosmosClient(string accountEndpointUri, string authKeyOrResourceToken, Func<dynamic[], CosmosClient> func)
        {
            if (CosmosClient == null || !accountEndpointUri.Equals(EndpointUri) || !authKeyOrResourceToken.Equals(AuthKeyOrResourceToken))
                IsFirstTimeCosmosClientCreation = true;

            bool _isFirstTime = IsFirstTimeCosmosClientCreation;

            CosmosClient response;
            var result = TryCreateResource(func, new dynamic[] { accountEndpointUri, authKeyOrResourceToken }, ref _isFirstTime, out response);

            IsFirstTimeCosmosClientCreation = _isFirstTime;

            if (result)
            {
                CosmosClient = response;
                EndpointUri = accountEndpointUri;
                AuthKeyOrResourceToken = authKeyOrResourceToken;
            }
        }

        protected virtual CosmosClient CreateCosmosClient(dynamic[] @params)
        {
            return CreateCosmosClient(@params[0], @params[1]);
        }

        protected virtual CosmosClient CreateCosmosClient(string accountEndpointUri, string authKeyOrResourceToken)
        {
            return new CosmosClient(accountEndpointUri, authKeyOrResourceToken);
        }

        #endregion

        #region Database creator

        /// <summary>
        /// Check if a database exists and create database if does not exists.
        /// The database id is used to verify if there is an existing database.
        /// </summary>
        /// <param name="databaseId">The database id.</param>
        protected virtual void CreateOrLoadDatabase(string databaseId)
        {
            ThrowIfInvalidDatabaseId(databaseId);

            ThrowIfInvalidCosmosClient();

            CreateOrLoadDatabase(databaseId, CreateDatabaseIfNotExists);
        }

        private void CreateOrLoadDatabase(string databaseId, Func<dynamic[], DatabaseResponse> func)
        {
            if (Database == null || !databaseId.Equals(Database.Id))
                IsFirstTimeDatabaseCreation = true;

            bool _isFirstTime = IsFirstTimeDatabaseCreation;

            DatabaseResponse response;
            var result = TryCreateResource(func, new dynamic[] { databaseId }, ref _isFirstTime, out response);

            IsFirstTimeDatabaseCreation = _isFirstTime;

            if (result && ResponseValidator.CreateResourceCosmosResponseSucceeded<Response<DatabaseProperties>, DatabaseProperties>(response))
                Database = response;
        }

        private DatabaseResponse CreateDatabaseIfNotExists(dynamic[] @params)
        {
            return CreateDatabaseIfNotExists(/*(string)*/@params[0]);
        }

        private DatabaseResponse CreateDatabaseIfNotExists(string databaseId)
        {
            return CosmosClient.CreateDatabaseIfNotExistsAsync(databaseId).WaitAndUnwrapException();
        }

        #endregion

        #region Container creator

        /// <summary>
        /// Check if a container exists and create container if does not exists.
        /// This will make a read operation, and if the container is not found it will do a create operation.
        /// </summary>
        /// <param name="containerId">The Cosmos container id</param>
        /// <param name="partitionKeyPath">The path to the partition key. Example: /PartitionKey</param>
        protected virtual void CreateOrLoadContainer(string containerId, string partitionKeyPath)
        {
            ThrowIfInvalidContainerId(containerId);

            if (!string.IsNullOrEmpty(partitionKeyPath) && !partitionKeyPath.StartsWith(ConstProvider.CosmosPartitionKeyPathStartPattern))
                partitionKeyPath = ConstProvider.CosmosPartitionKeyPathStartPattern + partitionKeyPath;

            ThrowIfInvalidPartitionKeyPath(partitionKeyPath);

            ThrowIfInvalidDatabase();

            CreateOrLoadContainer(containerId, partitionKeyPath, CreateContainerIfNotExists);
        }

        private void CreateOrLoadContainer(string containerId, string partitionKeyPath, Func<dynamic[], ContainerResponse> func)
        {
            if (Container == null || !containerId.Equals(Container.Id) || !partitionKeyPath.Equals(PartitionKeyPath))
                IsFirstTimeDatabaseCreation = true;

            bool _isFirstTime = IsFirstTimeDatabaseCreation;

            ContainerResponse response;
            var result = TryCreateResource(func, new dynamic[] { containerId, partitionKeyPath },
                ref _isFirstTime, out response);

            IsFirstTimeDatabaseCreation = _isFirstTime;

            if (result && ResponseValidator.CreateResourceCosmosResponseSucceeded<Response<ContainerProperties>, ContainerProperties>(response))
            {
                Container = response;
                PartitionKeyPath = partitionKeyPath;
            }
        }

        private ContainerResponse CreateContainerIfNotExists(dynamic[] @params)
        {
            return CreateContainerIfNotExists(@params[0], @params[1]);
        }

        private ContainerResponse CreateContainerIfNotExists(string containerId, string partitionKeyPath)
        {
            return Database.CreateContainerIfNotExistsAsync(containerId, partitionKeyPath).WaitAndUnwrapException();
        }

        #endregion

        #region Add async

        public virtual async Task<AzCosmosResponse<TIn>> AddEntityAsync<TIn>(TIn entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null) where TIn : BaseCosmosEntity
        {
            return await AddEntityAsync<TIn, AzCosmosResponse<TIn>>(entity, cancellationToken, databaseId,
                containerId);
        }

        public virtual async Task<TOut> AddEntityAsync<TIn, TOut>(TIn entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null) where TIn : BaseCosmosEntity where TOut : AzCosmosResponse<TIn>, new()
        {
            ThrowIfInvalidEntity(entity);

            return await AddEntityAsync<TIn, TOut>(entity, entity.PartitionKey, cancellationToken, databaseId,
                containerId, ConstProvider.DefaultCosmosPartitionKeyPath);
        }

        public virtual async Task<AzCosmosResponse<TIn>> AddEntityAsync<TIn>(TIn entity,
            string partitionKey,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return await AddEntityAsync<TIn, AzCosmosResponse<TIn>>(entity, partitionKey, cancellationToken, databaseId,
                containerId, partitionKeyPropName);
        }

        public virtual async Task<TOut> AddEntityAsync<TIn, TOut>(TIn entity,
            string partitionKey,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TOut : AzCosmosResponse<TIn>, new()
        {
            ThrowIfInvalidPartitionKeyValue(partitionKey);

            Initialize(databaseId, containerId, partitionKeyPropName, false);

            return await CosmosFuncHelper.ExecuteAsync<TIn, PartitionKey?, ItemRequestOptions, CancellationToken, ItemResponse<TIn>, TOut, TIn>(
                Container.CreateItemAsync,
                entity, new PartitionKey(partitionKey), default, cancellationToken);
        }

        #endregion

        #region Get async

        public virtual async Task<AzCosmosResponse<T>> GetEntityByIdAsync<T>(string partitionKey,
            string id,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return await GetEntityByIdAsync<T, AzCosmosResponse<T>>(partitionKey,
                id, cancellationToken, databaseId, containerId, partitionKeyPropName);
        }
        
        public virtual async Task<TOut> GetEntityByIdAsync<T, TOut>(string partitionKey, 
            string id,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TOut : AzCosmosResponse<T>, new()
        {
            ThrowIfInvalidPartitionKeyValue(partitionKey);
            ThrowIfInvalidId(id);

            Initialize(databaseId, containerId, partitionKeyPropName, false);

            return await CosmosFuncHelper.ExecuteAsync<string, PartitionKey, ItemRequestOptions, CancellationToken, ItemResponse<T>, TOut, T>(
                Container.ReadItemAsync<T>,
                id, new PartitionKey(partitionKey), default, cancellationToken);
        }

        #endregion

        #region Update async

        public virtual async Task<AzCosmosResponse<TIn>> UpdateEntityAsync<TIn>(TIn entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TIn : BaseCosmosEntity
        {
            return await UpdateEntityAsync<TIn, AzCosmosResponse<TIn>>(entity,
                cancellationToken, databaseId, containerId, partitionKeyPropName);
        }

        public virtual async Task<TOut> UpdateEntityAsync<TIn, TOut>(TIn entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TIn : BaseCosmosEntity where TOut : AzCosmosResponse<TIn>, new()
        {
            ThrowIfInvalidEntity(entity);

            return await UpdateEntityAsync<TIn, TOut>(entity, entity.Id, entity.PartitionKey, 
                cancellationToken, databaseId, containerId, partitionKeyPropName);
        }
        
        public virtual async Task<AzCosmosResponse<TIn>> UpdateEntityAsync<TIn>(TIn entity,
            string id,
            string partitionKey,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return await UpdateEntityAsync<TIn, AzCosmosResponse<TIn>>(entity, id, partitionKey, 
                cancellationToken, databaseId, containerId, partitionKeyPropName);
        }

        public virtual async Task<TOut> UpdateEntityAsync<TIn, TOut>(TIn entity,
            string id,
            string partitionKey,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TOut : AzCosmosResponse<TIn>, new()
        {
            ThrowIfInvalidId(id);
            ThrowIfInvalidPartitionKeyValue(partitionKey);

            Initialize(databaseId, containerId, partitionKeyPropName, false);

            return await CosmosFuncHelper.ExecuteAsync<TIn, string, PartitionKey?, ItemRequestOptions, CancellationToken, ItemResponse<TIn>, TOut, TIn>(
                Container.ReplaceItemAsync,
                entity, id, new PartitionKey(partitionKey), default, cancellationToken);
        }

        #endregion

        #region Upsert async

        public virtual async Task<AzCosmosResponse<TIn>> UpsertEntityAsync<TIn>(TIn entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TIn : BaseCosmosEntity
        {
            return await UpsertEntityAsync<TIn, AzCosmosResponse<TIn>>(entity, cancellationToken,
                databaseId, containerId, partitionKeyPropName);
        }
        
        public virtual async Task<TOut> UpsertEntityAsync<TIn, TOut>(TIn entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TIn : BaseCosmosEntity where TOut : AzCosmosResponse<TIn>, new()
        {
            ThrowIfInvalidEntity(entity);

            return await UpsertEntityAsync<TIn, TOut>(entity, entity.PartitionKey, cancellationToken,
                databaseId, containerId, partitionKeyPropName);
        }
        
        public virtual async Task<AzCosmosResponse<TIn>> UpsertEntityAsync<TIn>(TIn entity,
            string partitionKey,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return await UpsertEntityAsync<TIn, AzCosmosResponse<TIn>>(entity, partitionKey, cancellationToken,
                databaseId, containerId, partitionKeyPropName);
        }
        
        public virtual async Task<TOut> UpsertEntityAsync<TIn, TOut>(TIn entity,
            string partitionKey,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TOut : AzCosmosResponse<TIn>, new()
        {
            ThrowIfInvalidPartitionKeyValue(partitionKey);

            Initialize(databaseId, containerId, partitionKeyPropName, false);

            return await CosmosFuncHelper.ExecuteAsync<TIn, PartitionKey?, ItemRequestOptions, CancellationToken, ItemResponse<TIn>, TOut, TIn>(
                Container.UpsertItemAsync,
                entity, new PartitionKey(partitionKey), default, cancellationToken);
        }

        #endregion
    }
}
