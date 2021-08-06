﻿using AzStorage.Repositories.Core;
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
using AzCoreTools.Extensions;
using CoreTools.Utilities;
using AzStorage.Core.Texting;
using System.Linq;

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

        protected virtual void ThrowIfContainerIdOrPartitionKeyPathNullOrEmpty(string containerId, string partitionKeyPath)
        {
            if (!string.IsNullOrEmpty(containerId) && string.IsNullOrEmpty(partitionKeyPath))
                throw new ArgumentException($"'{nameof(containerId)}' or '{nameof(partitionKeyPath)}' are null or empty");
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

        protected virtual void ThrowIfInvalidNameValueProperties(IEnumerable<KeyValuePair<string, string>> nameValueProperties)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(nameValueProperties, nameof(nameValueProperties), nameof(nameValueProperties));
            
            foreach (var nameValueProp in nameValueProperties)
                ThrowIfInvalidNameValueProperty(nameValueProp, nameof(nameValueProperties));
        }

        protected virtual void ThrowIfInvalidNameValueProperty(KeyValuePair<string, string> nameValueProperty, string paramName)
        {
            if (string.IsNullOrEmpty(nameValueProperty.Key) || string.IsNullOrWhiteSpace(nameValueProperty.Key))
                ExThrower.ST_ThrowArgumentException(paramName, ErrorTextProvider.Invalid_KeyValuePair_key_null_empty_whitespaces);
        }

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

        #region Protected & private methods

        protected virtual void Initialize(string accountEndpointUri,
            string authKeyOrResourceToken,
            string databaseId,
            string containerId,
            string partitionKeyPath)
        {
            CreateOrLoadCosmosClient(accountEndpointUri, authKeyOrResourceToken);
            Initialize(databaseId, containerId, partitionKeyPath, true, true);
        }

        private void Initialize(string databaseId,
            string containerId,
            string partitionKeyPath,
            bool mandatoryDatabaseId,
            bool mandatoryContainerIdPartitionKeyPath)
        {
            if (mandatoryDatabaseId || !string.IsNullOrEmpty(databaseId))
                CreateOrLoadDatabase(databaseId);

            if (mandatoryContainerIdPartitionKeyPath || !string.IsNullOrEmpty(containerId) || !string.IsNullOrEmpty(partitionKeyPath))
                CreateOrLoadContainer(containerId, partitionKeyPath);
        }

        protected virtual QueryDefinition GenerateQueryDefinition(object operationTerms, 
            BooleanOperator boolOperator)
        {
            return GenerateQueryDefinition(CoreTools.Helpers.Helper
                .GetNameValueProperties(operationTerms, operationTerms.GetType().GetProperties()), boolOperator);
        }
        
        protected virtual QueryDefinition GenerateQueryDefinition(
            IEnumerable<KeyValuePair<string, string>> nameValueProperties, 
            BooleanOperator boolOperator)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(nameValueProperties);

            Dictionary<string, KeyValuePair<string, string>> aliasKeyValueDict;
            var queryText = BuildQueryTextToQueryDefinition(nameValueProperties,
                boolOperator, out aliasKeyValueDict);
            var queryDefinition = new QueryDefinition(queryText);
            foreach (var nameValue in aliasKeyValueDict)
                queryDefinition.WithParameter($"@{nameValue.Key}", nameValue.Value.Value);

            return queryDefinition;
        }

        protected virtual string BuildQueryTextToQueryDefinition(
            IEnumerable<KeyValuePair<string, string>> nameValueProperties,
            BooleanOperator boolOperator, out Dictionary<string, KeyValuePair<string, string>> aliasKeyValueDict)
        {
            string queryText = BuildDefaultQueryPrefix();

            string queryvarName = ConstProvider.DefaultQueryVarName;
            string operatorText = $" {boolOperator} ";

            aliasKeyValueDict = BuildQueryAliasKeyValueDict(nameValueProperties);

            bool firstIteration = true;
            foreach (var aliasKeyValue in aliasKeyValueDict)
            {
                if (!firstIteration)
                    queryText += operatorText;
                else
                    firstIteration = false;

                queryText += $"{queryvarName}.{aliasKeyValue.Value.Key} = @{aliasKeyValue.Key}"; 
            }

            return queryText;
        }

        private Dictionary<string, KeyValuePair<string, string>> BuildQueryAliasKeyValueDict(IEnumerable<KeyValuePair<string, string>> nameValueProperties)
        {
            var aliasKeyValueDict = new Dictionary<string, KeyValuePair<string, string>>(nameValueProperties.Count());
            bool currentAliasAdded;
            string currentAlias;
            var r = new Random();
            foreach (var nameValueProp in nameValueProperties)
            {
                currentAliasAdded = false;
                currentAlias = nameValueProp.Key;
                do
                {
                    if (!aliasKeyValueDict.ContainsKey(currentAlias))
                    {
                        aliasKeyValueDict.Add(currentAlias, nameValueProp);
                        currentAliasAdded = true;
                    }
                    else
                        currentAlias += r.Next(1, int.MaxValue);

                } while (!currentAliasAdded);
            }

            return aliasKeyValueDict;
        }

        protected virtual string BuildDefaultQueryPrefix()
        {
            return ConstProvider.DefaultQueryPrefix;
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

        private string GetConsolidatedDefaultCosmosPartitionKeyPath(string containerId)
        {
            if (string.IsNullOrEmpty(containerId))
                return string.Empty;

            return ConstProvider.DefaultCosmosPartitionKeyPath;
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
            ThrowIfContainerIdOrPartitionKeyPathNullOrEmpty(containerId, partitionKeyPath);

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

        /// <summary>
        /// Adds a item of type <typeparamref name="TIn"/> as an asynchronous operation in the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <param name="entity">The item to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <returns>The <see cref="AzCosmosResponse<TIn>"/> indicating the result of the operation, that was created 
        /// contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.
        /// </returns>
        public virtual async Task<AzCosmosResponse<TIn>> AddEntityAsync<TIn>(TIn entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null) where TIn : BaseCosmosEntity
        {
            return await AddEntityAsync<TIn, AzCosmosResponse<TIn>>(entity, cancellationToken, databaseId,
                containerId);
        }

        /// <summary>
        /// Adds a item of type <typeparamref name="TIn"/> as an asynchronous operation in the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <typeparam name="TOut">A custom model type that inherits from <see cref="AzCosmosResponse<TIn>" />.</typeparam>
        /// <param name="entity">The item to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <returns>The <see cref="TOut"/> indicating the result of the operation, that was created 
        /// contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.
        /// </returns>
        public virtual async Task<TOut> AddEntityAsync<TIn, TOut>(TIn entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null) where TIn : BaseCosmosEntity where TOut : AzCosmosResponse<TIn>, new()
        {
            ThrowIfInvalidEntity(entity);

            return await AddEntityAsync<TIn, TOut>(entity, entity.PartitionKey, cancellationToken, databaseId,
                containerId, GetConsolidatedDefaultCosmosPartitionKeyPath(containerId));
        }

        /// <summary>
        /// Adds a item of type <typeparamref name="TIn"/> as an asynchronous operation in the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="TIn">A custom model type.</typeparam>
        /// <param name="entity">The item to add.</param>
        /// <param name="partitionKey">The value to use as partition key</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /location</param>
        /// <returns>The <see cref="AzCosmosResponse<TIn>"/> indicating the result of the operation, that was created 
        /// contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.
        /// </returns>
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

        /// <summary>
        /// Adds a item of type <typeparamref name="TIn"/> as an asynchronous operation in the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="TIn">A custom model type.</typeparam>
        /// <typeparam name="TOut">A custom model type that inherits from <see cref="AzCosmosResponse<TIn>" />.</typeparam>
        /// <param name="entity">The item to add.</param>
        /// <param name="partitionKey">The value to use as partition key</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /location</param>
        /// <returns>The <see cref="TOut"/> indicating the result of the operation, that was created 
        /// contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.
        /// </returns>
        public virtual async Task<TOut> AddEntityAsync<TIn, TOut>(TIn entity,
            string partitionKey,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TOut : AzCosmosResponse<TIn>, new()
        {
            ThrowIfInvalidPartitionKeyValue(partitionKey);

            Initialize(databaseId, containerId, partitionKeyPropName, false, false);

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

            Initialize(databaseId, containerId, partitionKeyPropName, false, false);

            return await CosmosFuncHelper.ExecuteAsync<string, PartitionKey, ItemRequestOptions, CancellationToken, ItemResponse<T>, TOut, T>(
                Container.ReadItemAsync<T>,
                id, new PartitionKey(partitionKey), default, cancellationToken);
        }

        #endregion

        #region Query

        #region QueryAll

        public virtual AzCosmosResponse<List<T>> QueryAll<T>(
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return QueryAll<T, AzCosmosResponse<List<T>>>(databaseId,
                containerId,
                partitionKeyPropName);
        }

        public virtual TOut QueryAll<T, TOut>(
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TOut : AzCosmosResponse<List<T>>, new()
        {
            Initialize(databaseId, containerId, partitionKeyPropName, false, false);

            return CosmosFuncHelper.Execute<int, AzCosmosResponse<List<T>>, TOut, List<T>>(
                Container.QueryAll<T>,
                int.MaxValue);
        }

        #endregion

        #region LazyQueryAll

        public virtual AzCosmosResponse<IEnumerable<T>> LazyQueryAll<T>(
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return LazyQueryAll<T, AzCosmosResponse<IEnumerable<T>>>(
                databaseId,
                containerId,
                partitionKeyPropName);
        }

        public virtual TOut LazyQueryAll<T, TOut>(
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TOut : AzCosmosResponse<IEnumerable<T>>, new()
        {
            Initialize(databaseId, containerId, partitionKeyPropName, false, false);

            return CosmosFuncHelper.Execute<int, AzCosmosResponse<IEnumerable<T>>, TOut, IEnumerable<T>>(
                Container.LazyQueryAll<T>,
                int.MaxValue);
        }

        #endregion

        #region QueryByFilter

        public virtual AzCosmosResponse<List<T>> QueryByFilter<T>(
            string filter,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return QueryByFilter<T, AzCosmosResponse<List<T>>>(
                filter,
                databaseId,
                containerId,
                partitionKeyPropName);
        }

        public virtual TOut QueryByFilter<T, TOut>(
            string filter,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TOut : AzCosmosResponse<List<T>>, new()
        {
            Initialize(databaseId, containerId, partitionKeyPropName, false, false);

            return CosmosFuncHelper.Execute<string, int, AzCosmosResponse<List<T>>, TOut, List<T>>(
                Container.QueryByFilter<T>,
                filter,
                int.MaxValue);
        }

        #endregion

        #region LazyQueryByFilter

        public virtual AzCosmosResponse<IEnumerable<T>> LazyQueryByFilter<T>(
            string filter,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return LazyQueryByFilter<T, AzCosmosResponse<IEnumerable<T>>>(
                filter,
                databaseId,
                containerId,
                partitionKeyPropName);
        }

        public virtual TOut LazyQueryByFilter<T, TOut>(
            string filter,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TOut : AzCosmosResponse<IEnumerable<T>>, new()
        {
            Initialize(databaseId, containerId, partitionKeyPropName, false, false);

            return CosmosFuncHelper.Execute<string, int, AzCosmosResponse<IEnumerable<T>>, TOut, IEnumerable<T>>(
                Container.LazyQueryByFilter<T>,
                filter,
                int.MaxValue);
        }

        #endregion

        #region QueryByQueryDefinition

        public virtual AzCosmosResponse<List<T>> QueryByQueryDefinition<T>(
            QueryDefinition queryDefinition,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return QueryByQueryDefinition<T, AzCosmosResponse<List<T>>>(
                queryDefinition,
                databaseId,
                containerId,
                partitionKeyPropName);
        }

        public virtual TOut QueryByQueryDefinition<T, TOut>(
            QueryDefinition queryDefinition,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TOut : AzCosmosResponse<List<T>>, new()
        {
            Initialize(databaseId, containerId, partitionKeyPropName, false, false);

            return CosmosFuncHelper.Execute<QueryDefinition, int, AzCosmosResponse<List<T>>, TOut, List<T>>(
                Container.QueryByQueryDefinition<T>,
                queryDefinition,
                int.MaxValue);
        }

        #endregion

        #region LazyQueryByQueryDefinition

        public virtual AzCosmosResponse<IEnumerable<T>> LazyQueryByQueryDefinition<T>(
            QueryDefinition queryDefinition,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return LazyQueryByQueryDefinition<T, AzCosmosResponse<IEnumerable<T>>>(
                queryDefinition,
                databaseId,
                containerId,
                partitionKeyPropName);
        }

        public virtual TOut LazyQueryByQueryDefinition<T, TOut>(
            QueryDefinition queryDefinition,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TOut : AzCosmosResponse<IEnumerable<T>>, new()
        {
            Initialize(databaseId, containerId, partitionKeyPropName, false, false);

            return CosmosFuncHelper.Execute<QueryDefinition, int, AzCosmosResponse<IEnumerable<T>>, TOut, IEnumerable<T>>(
                Container.LazyQueryByQueryDefinition<T>,
                queryDefinition,
                int.MaxValue);
        }

        #endregion

        #region QueryByPartitionKey

        public virtual AzCosmosResponse<List<T>> QueryByPartitionKey<T>(
            string partitionKey,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return QueryByPartitionKey<T, AzCosmosResponse<List<T>>>(
                partitionKey,
                databaseId,
                containerId,
                partitionKeyPropName);
        }

        public virtual TOut QueryByPartitionKey<T, TOut>(
            string partitionKey,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TOut : AzCosmosResponse<List<T>>, new()
        {
            Initialize(databaseId, containerId, partitionKeyPropName, false, false);

            return CosmosFuncHelper.Execute<string, int, AzCosmosResponse<List<T>>, TOut, List<T>>(
                Container.QueryByPartitionKey<T>,
                partitionKey,
                int.MaxValue);
        }

        #endregion

        #region LazyQueryByPartitionKey

        public virtual AzCosmosResponse<IEnumerable<T>> LazyQueryByPartitionKey<T>(
            string partitionKey,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return LazyQueryByPartitionKey<T, AzCosmosResponse<IEnumerable<T>>>(
                partitionKey,
                databaseId,
                containerId,
                partitionKeyPropName);
        }

        public virtual TOut LazyQueryByPartitionKey<T, TOut>(
            string partitionKey,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TOut : AzCosmosResponse<IEnumerable<T>>, new()
        {
            Initialize(databaseId, containerId, partitionKeyPropName, false, false);

            return CosmosFuncHelper.Execute<string, int, AzCosmosResponse<IEnumerable<T>>, TOut, IEnumerable<T>>(
                Container.LazyQueryByPartitionKey<T>,
                partitionKey,
                int.MaxValue);
        }

        #endregion

        #region QueryWithOr

        public virtual AzCosmosResponse<List<T>> QueryWithOr<T>(
            IEnumerable<KeyValuePair<string, string>> nameValueProperties,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return QueryWithOr<T, AzCosmosResponse<List<T>>>(
                nameValueProperties,
                databaseId,
                containerId,
                partitionKeyPropName);
        }
        
        public virtual TOut QueryWithOr<T, TOut>(
            IEnumerable<KeyValuePair<string, string>> nameValueProperties,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TOut : AzCosmosResponse<List<T>>, new()
        {
            ThrowIfInvalidNameValueProperties(nameValueProperties);

            return QueryByQueryDefinition<T, TOut>(GenerateQueryDefinition(nameValueProperties, BooleanOperator.or),
                databaseId, containerId, partitionKeyPropName);
        }

        public virtual AzCosmosResponse<List<T>> QueryWithOr<T>(
            dynamic operationTerms,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return QueryWithOr<T, AzCosmosResponse<List<T>>>(
                operationTerms,
                databaseId,
                containerId,
                partitionKeyPropName);
        }

        public virtual TOut QueryWithOr<T, TOut>(
            dynamic operationTerms,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TOut : AzCosmosResponse<List<T>>, new()
        {
            ExThrower.ST_ThrowIfArgumentIsNull(operationTerms, nameof(operationTerms), nameof(operationTerms));

            return QueryByQueryDefinition<T, TOut>(GenerateQueryDefinition(operationTerms, BooleanOperator.or), 
                databaseId, containerId, partitionKeyPropName);
        }
        
        #endregion
        
        #region LazyQueryWithOr

        public virtual AzCosmosResponse<IEnumerable<T>> LazyQueryWithOr<T>(
            IEnumerable<KeyValuePair<string, string>> nameValueProperties,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return LazyQueryWithOr<T, AzCosmosResponse<IEnumerable<T>>>(
                nameValueProperties,
                databaseId,
                containerId,
                partitionKeyPropName);
        }
        
        public virtual TOut LazyQueryWithOr<T, TOut>(
            IEnumerable<KeyValuePair<string, string>> nameValueProperties,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TOut : AzCosmosResponse<IEnumerable<T>>, new()
        {
            ThrowIfInvalidNameValueProperties(nameValueProperties);

            return LazyQueryByQueryDefinition<T, TOut>(GenerateQueryDefinition(nameValueProperties, BooleanOperator.or),
                databaseId, containerId, partitionKeyPropName);
        }

        public virtual AzCosmosResponse<IEnumerable<T>> LazyQueryWithOr<T>(
            dynamic operationTerms,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return LazyQueryWithOr<T, AzCosmosResponse<IEnumerable<T>>>(
                operationTerms,
                databaseId,
                containerId,
                partitionKeyPropName);
        }

        public virtual TOut LazyQueryWithOr<T, TOut>(
            dynamic operationTerms,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TOut : AzCosmosResponse<IEnumerable<T>>, new()
        {
            ExThrower.ST_ThrowIfArgumentIsNull(operationTerms, nameof(operationTerms), nameof(operationTerms));

            return LazyQueryByQueryDefinition<T, TOut>(GenerateQueryDefinition(operationTerms, BooleanOperator.or), 
                databaseId, containerId, partitionKeyPropName);
        }

        #endregion

        #region QueryWithAnd

        public virtual AzCosmosResponse<List<T>> QueryWithAnd<T>(
            IEnumerable<KeyValuePair<string, string>> nameValueProperties,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return QueryWithAnd<T, AzCosmosResponse<List<T>>>(
                nameValueProperties,
                databaseId,
                containerId,
                partitionKeyPropName);
        }

        public virtual TOut QueryWithAnd<T, TOut>(
            IEnumerable<KeyValuePair<string, string>> nameValueProperties,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TOut : AzCosmosResponse<List<T>>, new()
        {
            ThrowIfInvalidNameValueProperties(nameValueProperties);

            return QueryByQueryDefinition<T, TOut>(GenerateQueryDefinition(nameValueProperties, BooleanOperator.and),
                databaseId, containerId, partitionKeyPropName);
        }

        public virtual AzCosmosResponse<List<T>> QueryWithAnd<T>(
            dynamic operationTerms,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return QueryWithAnd<T, AzCosmosResponse<List<T>>>(
                operationTerms,
                databaseId,
                containerId,
                partitionKeyPropName);
        }

        public virtual TOut QueryWithAnd<T, TOut>(
            dynamic operationTerms,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TOut : AzCosmosResponse<List<T>>, new()
        {
            ExThrower.ST_ThrowIfArgumentIsNull(operationTerms, nameof(operationTerms), nameof(operationTerms));

            return QueryByQueryDefinition<T, TOut>(GenerateQueryDefinition(operationTerms, BooleanOperator.and), 
                databaseId, containerId, partitionKeyPropName);
        }

        #endregion
        
        #region LazyQueryWithAnd

        public virtual AzCosmosResponse<IEnumerable<T>> LazyQueryWithAnd<T>(
            IEnumerable<KeyValuePair<string, string>> nameValueProperties,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return LazyQueryWithAnd<T, AzCosmosResponse<IEnumerable<T>>>(
                nameValueProperties,
                databaseId,
                containerId,
                partitionKeyPropName);
        }

        public virtual TOut LazyQueryWithAnd<T, TOut>(
            IEnumerable<KeyValuePair<string, string>> nameValueProperties,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TOut : AzCosmosResponse<IEnumerable<T>>, new()
        {
            ThrowIfInvalidNameValueProperties(nameValueProperties);

            return LazyQueryByQueryDefinition<T, TOut>(GenerateQueryDefinition(nameValueProperties, BooleanOperator.and),
                databaseId, containerId, partitionKeyPropName);
        }

        public virtual AzCosmosResponse<IEnumerable<T>> LazyQueryWithAnd<T>(
            dynamic operationTerms,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return LazyQueryWithAnd<T, AzCosmosResponse<IEnumerable<T>>>(
                operationTerms,
                databaseId,
                containerId,
                partitionKeyPropName);
        }

        public virtual TOut LazyQueryWithAnd<T, TOut>(
            dynamic operationTerms,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TOut : AzCosmosResponse<IEnumerable<T>>, new()
        {
            ExThrower.ST_ThrowIfArgumentIsNull(operationTerms, nameof(operationTerms), nameof(operationTerms));

            return LazyQueryByQueryDefinition<T, TOut>(GenerateQueryDefinition(operationTerms, BooleanOperator.and), 
                databaseId, containerId, partitionKeyPropName);
        }

        #endregion

        #endregion

        #region Update async

        public virtual async Task<AzCosmosResponse<TIn>> UpdateEntityAsync<TIn>(TIn entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null) where TIn : BaseCosmosEntity
        {
            return await UpdateEntityAsync<TIn, AzCosmosResponse<TIn>>(entity,
                cancellationToken, databaseId, containerId);
        }

        public virtual async Task<TOut> UpdateEntityAsync<TIn, TOut>(TIn entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null) where TIn : BaseCosmosEntity where TOut : AzCosmosResponse<TIn>, new()
        {
            ThrowIfInvalidEntity(entity);

            return await UpdateEntityAsync<TIn, TOut>(entity, entity.Id, entity.PartitionKey,
                cancellationToken, databaseId, containerId, GetConsolidatedDefaultCosmosPartitionKeyPath(containerId));
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

            Initialize(databaseId, containerId, partitionKeyPropName, false, false);

            return await CosmosFuncHelper.ExecuteAsync<TIn, string, PartitionKey?, ItemRequestOptions, CancellationToken, ItemResponse<TIn>, TOut, TIn>(
                Container.ReplaceItemAsync,
                entity, id, new PartitionKey(partitionKey), default, cancellationToken);
        }

        #endregion

        #region Upsert async

        public virtual async Task<AzCosmosResponse<TIn>> UpsertEntityAsync<TIn>(TIn entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null) where TIn : BaseCosmosEntity
        {
            return await UpsertEntityAsync<TIn, AzCosmosResponse<TIn>>(entity, cancellationToken,
                databaseId, containerId);
        }

        public virtual async Task<TOut> UpsertEntityAsync<TIn, TOut>(TIn entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null) where TIn : BaseCosmosEntity where TOut : AzCosmosResponse<TIn>, new()
        {
            ThrowIfInvalidEntity(entity);

            return await UpsertEntityAsync<TIn, TOut>(entity, entity.PartitionKey, cancellationToken,
                databaseId, containerId, GetConsolidatedDefaultCosmosPartitionKeyPath(containerId));
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

            Initialize(databaseId, containerId, partitionKeyPropName, false, false);

            return await CosmosFuncHelper.ExecuteAsync<TIn, PartitionKey?, ItemRequestOptions, CancellationToken, ItemResponse<TIn>, TOut, TIn>(
                Container.UpsertItemAsync,
                entity, new PartitionKey(partitionKey), default, cancellationToken);
        }

        #endregion

        #region Delete async
        
        public virtual async Task<AzCosmosResponse<T>> DeleteEntityAsync<T>(T entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null) where T : BaseCosmosEntity
        {
            return await DeleteEntityAsync<T, AzCosmosResponse<T>>(entity, cancellationToken,
                databaseId, containerId);
        }
        
        public virtual async Task<TOut> DeleteEntityAsync<T, TOut>(T entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null) where T : BaseCosmosEntity where TOut : AzCosmosResponse<T>, new()
        {
            ThrowIfInvalidEntity(entity);

            return await DeleteEntityAsync<T, TOut>(entity.Id, entity.PartitionKey, cancellationToken,
                databaseId, containerId, GetConsolidatedDefaultCosmosPartitionKeyPath(containerId));
        }
        
        public virtual async Task<AzCosmosResponse<T>> DeleteEntityAsync<T>(string id,
            string partitionKey,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return await DeleteEntityAsync<T, AzCosmosResponse<T>>(id, partitionKey, cancellationToken,
                databaseId, containerId, partitionKeyPropName);
        }
        
        public virtual async Task<TOut> DeleteEntityAsync<T, TOut>(string id,
            string partitionKey,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TOut : AzCosmosResponse<T>, new()
        {
            ThrowIfInvalidId(id);
            ThrowIfInvalidPartitionKeyValue(partitionKey);

            Initialize(databaseId, containerId, partitionKeyPropName, false, false);

            return await CosmosFuncHelper.ExecuteAsync<string, PartitionKey, ItemRequestOptions, CancellationToken, ItemResponse<T>, TOut, T>(
                Container.DeleteItemAsync<T>,
                id, new PartitionKey(partitionKey), default, cancellationToken);
        }

        #endregion
    }
}
