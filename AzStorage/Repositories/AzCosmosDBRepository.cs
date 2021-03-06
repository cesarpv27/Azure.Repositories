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
using AzCoreTools.Extensions;
using CoreTools.Utilities;
using AzStorage.Core.Texting;
using System.Linq;
using AzStorage.Core.Extensions;
using System.IO;

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
            CreateResourcePolicy createResourcePolicy = CreateResourcePolicy.OnlyFirstTime,
            AzCosmosRetryOptions retryOptions = null) : base(createResourcePolicy, retryOptions)
        {
            InitializeForcingMandatory(accountEndpointUri, authKeyOrResourceToken, databaseId, containerId, partitionKeyPropName);
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
            ExThrower.ST_ThrowIfArgumentIsNullOrWhitespace(accountEndpointUri, nameof(accountEndpointUri), nameof(accountEndpointUri));
        }

        protected virtual void ThrowIfInvalidAuthKeyOrResourceToken(string authKeyOrResourceToken)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrWhitespace(authKeyOrResourceToken, 
                nameof(authKeyOrResourceToken), nameof(authKeyOrResourceToken));
        }

        protected virtual void ThrowIfInvalidDatabaseId(string databaseId)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrWhitespace(databaseId, nameof(databaseId), nameof(databaseId));
        }

        protected virtual void ThrowIfContainerIdOrPartitionKeyPathNullOrEmpty(string containerId, string partitionKeyPath)
        {
            if (!string.IsNullOrEmpty(containerId) && string.IsNullOrEmpty(partitionKeyPath))
                throw new ArgumentException($"'{nameof(containerId)}' or '{nameof(partitionKeyPath)}' are null or empty");
        }

        protected virtual void ThrowIfInvalidContainerId(string containerId)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrWhitespace(containerId, nameof(containerId), nameof(containerId));
        }

        protected virtual void ThrowIfInvalidPartitionKeyPath(string partitionKeyPath)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrWhitespace(partitionKeyPath, nameof(partitionKeyPath), nameof(partitionKeyPath));
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

        protected virtual void ThrowIfInvalidPartitionKeyValue(string partitionKey)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrWhitespace(partitionKey, nameof(partitionKey), nameof(partitionKey));
        }
        
        protected virtual void ThrowIfInvalidPartitionKeyValue<T>(T entity, Func<T, string> funcGetPartitionKey)
        {
            ThrowIfInvalidPartitionKeyValue(funcGetPartitionKey(entity));
        }

        protected virtual void ThrowIfInvalidId(string id)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrWhitespace(id, nameof(id), nameof(id));
        }

        protected virtual void ThrowIfInvalidId<T>(T entity, Func<T, string> funcGetId)
        {
            if (funcGetId != null)
                ThrowIfInvalidId(funcGetId(entity));
        }

        protected virtual void ThrowIfInvalidEntity<T>(T entity) where T : BaseCosmosEntity
        {
            ExThrower.ST_ThrowIfArgumentIsNull(entity, nameof(entity));

            ThrowIfInvalidPartitionKeyValue(entity.PartitionKey);
            ThrowIfInvalidId(entity.Id);
        }
        
        protected virtual void ThrowIfInvalidEntity<T>(T entity, 
            Func<T, string> funcGetPartitionKey, 
            Func<T, string> funcGetId)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(entity, nameof(entity));

            ThrowIfInvalidPartitionKeyValue(entity, funcGetPartitionKey);
            ThrowIfInvalidId(entity, funcGetId);
        }

        protected virtual void ThrowIfContainsInvalidEntity<T>(IEnumerable<T> entities) where T : BaseCosmosEntity
        {
            ExThrower.ST_ThrowIfArgumentIsNull(entities, nameof(entities));

            foreach (var _entt in entities)
                ThrowIfInvalidEntity(_entt);
        }
        
        protected virtual void ThrowIfContainsInvalidEntity<T>(IEnumerable<T> entities,
            Func<T, string> funcGetPartitionKey,
            Func<T, string> funcGetId)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(entities, nameof(entities));

            foreach (var _entt in entities)
                ThrowIfInvalidEntity(_entt, funcGetPartitionKey, funcGetId);
        }

        #endregion

        #region Protected & private methods

        protected virtual void InitializeForcingMandatory(string accountEndpointUri,
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

        protected virtual AzCosmosTransactionStore<T> CreateAzCosmosTransactionStore<T>(Func<T, string> funcGetPartitionKey)
        {
            return new AzCosmosTransactionStore<T>(funcGetPartitionKey);
        }

        protected virtual async Task<List<AzCosmosResponse<TransactionalBatchResponse>>> ExecuteOperationTransactionallyAsync<TIn>(
            IEnumerable<TIn> entities,
            Action<AzCosmosTransactionStore<TIn>, IEnumerable<TIn>> actFillTransactionStore,
            Func<TIn, string> funcGetPartitionKey,
            Func<TIn, string> funcGetId = default,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(entities, nameof(entities));
            ExThrower.ST_ThrowIfArgumentIsNull(funcGetPartitionKey, nameof(funcGetPartitionKey));

            ThrowIfContainsInvalidEntity(entities, funcGetPartitionKey, funcGetId);

            Initialize(databaseId, containerId, partitionKeyPropName, false, false);

            var azTableTransactionStore = CreateAzCosmosTransactionStore(funcGetPartitionKey);
            actFillTransactionStore(azTableTransactionStore, entities);

            return await Container.SubmitTransactionAsync(azTableTransactionStore,
                funcGetPartitionKey, funcGetId, cancellationToken);
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
            return CreateDatabaseIfNotExists(@params[0]);
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

        #region Add

        /// <summary>
        /// Adds a item of type <typeparamref name="TIn"/> into the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <param name="entity">The item to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <returns>The <see cref="AzCosmosResponse{TIn}"/> indicating the result of the operation.</returns>
        public virtual AzCosmosResponse<TIn> AddEntity<TIn>(TIn entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null) where TIn : BaseCosmosEntity
        {
            return AddEntity<TIn, AzCosmosResponse<TIn>>(entity, cancellationToken, databaseId,
                containerId);
        }

        /// <summary>
        /// Adds a item of type <typeparamref name="TIn"/> into the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{TIn}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{TIn}" />.</typeparam>
        /// <param name="entity">The item to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <returns>The <see cref="TOut"/> indicating the result of the operation.</returns>
        public virtual TOut AddEntity<TIn, TOut>(TIn entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null) where TIn : BaseCosmosEntity where TOut : AzCosmosResponse<TIn>, new()
        {
            ThrowIfInvalidEntity(entity);

            return AddEntity<TIn, TOut>(entity, entity.PartitionKey, cancellationToken, databaseId,
                containerId, GetConsolidatedDefaultCosmosPartitionKeyPath(containerId));
        }

        /// <summary>
        /// Adds a item of type <typeparamref name="TIn"/> into the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="TIn">A custom model type.</typeparam>
        /// <param name="entity">The item to add.</param>
        /// <param name="partitionKey">The value to use as partition key.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>The <see cref="AzCosmosResponse{TIn}"/> indicating the result of the operation.</returns>
        public virtual AzCosmosResponse<TIn> AddEntity<TIn>(TIn entity,
            string partitionKey,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return AddEntity<TIn, AzCosmosResponse<TIn>>(entity, partitionKey, cancellationToken, databaseId,
                containerId, partitionKeyPropName);
        }

        /// <summary>
        /// Adds a item of type <typeparamref name="TIn"/> into the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="TIn">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{TIn}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{TIn}" />.</typeparam>
        /// <param name="entity">The item to add.</param>
        /// <param name="partitionKey">The value to use as partition key</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>The <see cref="TOut"/> indicating the result of the operation.</returns>
        public virtual TOut AddEntity<TIn, TOut>(TIn entity,
            string partitionKey,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TOut : AzCosmosResponse<TIn>, new()
        {
            return AddEntityAsync<TIn, TOut>(
                entity,
                partitionKey,
                cancellationToken,
                databaseId,
                containerId,
                partitionKeyPropName).WaitAndUnwrapException();
        }

        #endregion

        #region Add async

        /// <summary>
        /// Adds a item of type <typeparamref name="TIn"/> as an asynchronous operation in the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <param name="entity">The item to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <returns>The <see cref="AzCosmosResponse{TIn}"/> indicating the result of the operation, that was created 
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
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{TIn}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{TIn}" />.</typeparam>
        /// <param name="entity">The item to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
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
        /// <param name="partitionKey">The value to use as partition key.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>The <see cref="AzCosmosResponse{TIn}"/> indicating the result of the operation, that was created 
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
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{TIn}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{TIn}" />.</typeparam>
        /// <param name="entity">The item to add.</param>
        /// <param name="partitionKey">The value to use as partition key</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
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

        #region Add transactionally

        /// <summary>
        /// Adds entities in one or more batch transactions into the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{TransactionalBatchResponse}"/>
        /// with information about each batch execution.</returns>
        public virtual List<AzCosmosResponse<TransactionalBatchResponse>> AddEntitiesTransactionally<TIn>(
            IEnumerable<TIn> entities,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
            where TIn : BaseCosmosEntity
        {
            return AddEntitiesTransactionally(entities, entt => entt.PartitionKey,
                cancellationToken, databaseId, containerId, partitionKeyPropName);
        }

        /// <summary>
        /// Adds entities in one or more batch transactions into the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="TIn">A custom model type.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="funcGetPartitionKey">Delegate that returns the entity partition key.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{TransactionalBatchResponse}"/>
        /// with information about each batch execution.</returns>
        public virtual List<AzCosmosResponse<TransactionalBatchResponse>> AddEntitiesTransactionally<TIn>(
            IEnumerable<TIn> entities,
            Func<TIn, string> funcGetPartitionKey,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return AddEntitiesTransactionallyAsync(entities, funcGetPartitionKey, cancellationToken,
                databaseId, containerId, partitionKeyPropName).WaitAndUnwrapException();
        }

        #endregion

        #region Add transactionally async

        /// <summary>
        /// Adds entities in one or more batch transactions into the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{TransactionalBatchResponse}"/>
        /// with information about each batch execution, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<List<AzCosmosResponse<TransactionalBatchResponse>>> AddEntitiesTransactionallyAsync<TIn>(
            IEnumerable<TIn> entities,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
            where TIn : BaseCosmosEntity
        {
            return await AddEntitiesTransactionallyAsync(entities, entt => entt.PartitionKey,
                cancellationToken, databaseId, containerId, partitionKeyPropName);
        }

        /// <summary>
        /// Adds entities in one or more batch transactions into the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="TIn">A custom model type.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="funcGetPartitionKey">Delegate that returns the entity partition key.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{TransactionalBatchResponse}"/>
        /// with information about each batch execution, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<List<AzCosmosResponse<TransactionalBatchResponse>>> AddEntitiesTransactionallyAsync<TIn>(
            IEnumerable<TIn> entities,
            Func<TIn, string> funcGetPartitionKey,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return await ExecuteOperationTransactionallyAsync(entities, 
                (azTableTransactionStore, entts) => azTableTransactionStore.ClearNAdd(entts),
                funcGetPartitionKey, default, cancellationToken,
                databaseId, containerId, partitionKeyPropName);
        }

        #endregion

        #region Get

        /// <summary>
        /// Reads a item of type <typeparamref name="T"/> from the Azure Cosmos service as an asynchronous operation.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="partitionKey">The partition key value of the item.</param>
        /// <param name="id">The Cosmos item id.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>The <see cref="AzCosmosResponse{T}"/> indicating the result of the operation and containing 
        /// a item of type <typeparamref name="T"/>.</returns>
        public virtual AzCosmosResponse<T> GetEntityById<T>(string partitionKey,
            string id,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return GetEntityById<T, AzCosmosResponse<T>>(partitionKey,
                id, cancellationToken, databaseId, containerId, partitionKeyPropName);
        }

        /// <summary>
        /// Reads a item of type <typeparamref name="T"/> from the Azure Cosmos service as an asynchronous operation.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{T}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{T}" />.</typeparam>
        /// <param name="partitionKey">The partition key value of the item.</param>
        /// <param name="id">The Cosmos item id.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>The <see cref="TOut"/> indicating the result of the operation and containing 
        /// a item of type <typeparamref name="T"/>.</returns>
        public virtual TOut GetEntityById<T, TOut>(string partitionKey,
            string id,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TOut : AzCosmosResponse<T>, new()
        {
            return GetEntityByIdAsync<T, TOut>(
                partitionKey,
                id,
                cancellationToken,
                databaseId,
                containerId,
                partitionKeyPropName).WaitAndUnwrapException();
        }

        #endregion

        #region Get async

        /// <summary>
        /// Reads a item of type <typeparamref name="T"/> from the Azure Cosmos service as an asynchronous operation.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="partitionKey">The partition key value of the item.</param>
        /// <param name="id">The Cosmos item id.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>The <see cref="AzCosmosResponse{T}"/> indicating the result of the operation and containing 
        /// a item of type <typeparamref name="T"/>. That response was created 
        /// contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.
        /// </returns>
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

        /// <summary>
        /// Reads a item of type <typeparamref name="T"/> from the Azure Cosmos service as an asynchronous operation.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{T}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{T}" />.</typeparam>
        /// <param name="partitionKey">The partition key value of the item.</param>
        /// <param name="id">The Cosmos item id.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>The <see cref="TOut"/> indicating the result of the operation and containing 
        /// a item of type <typeparamref name="T"/>. That response was created 
        /// contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.
        /// </returns>
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

        #region Get transactionally

        /// <summary>
        /// Gets entities in one or more batch transactions from the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="T">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="funcGetPartitionKey">Delegate that returns the entity partition key.</param>
        /// <param name="funcGetId">Delegate that returns the entity id.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{List{T}}"/>
        /// with information about each batch execution.</returns>
        public virtual List<AzCosmosResponse<List<T>>> GetEntitiesTransactionally<T>(
            IEnumerable<T> entities,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
            where T : BaseCosmosEntity
        {
            return GetEntitiesTransactionally(entities, entt => entt.PartitionKey, entt => entt.Id,
                cancellationToken, databaseId, containerId, partitionKeyPropName);
        }

        /// <summary>
        /// Gets entities in one or more batch transactions from the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="funcGetPartitionKey">Delegate that returns the entity partition key.</param>
        /// <param name="funcGetId">Delegate that returns the entity id.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{List{T}}"/>
        /// with information about each batch execution.</returns>
        public virtual List<AzCosmosResponse<List<T>>> GetEntitiesTransactionally<T>(
            IEnumerable<T> entities,
            Func<T, string> funcGetPartitionKey,
            Func<T, string> funcGetId,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return GetEntitiesTransactionallyAsync(entities, funcGetPartitionKey, funcGetId,
                cancellationToken, databaseId, containerId, partitionKeyPropName).WaitAndUnwrapException();
        }

        /// <summary>
        /// Gets entities as string in one or more batch transactions from the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="T">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{List{string}}"/>
        /// with information about each batch execution.</returns>
        public virtual List<AzCosmosResponse<List<string>>> GetEntitiesAsStringTransactionally<T>(
            IEnumerable<T> entities,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
            where T : BaseCosmosEntity
        {
            return GetEntitiesAsStringTransactionally(entities, entt => entt.PartitionKey, entt => entt.Id,
                cancellationToken, databaseId, containerId, partitionKeyPropName);
        }

        /// <summary>
        /// Gets entities as string in one or more batch transactions from the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="funcGetPartitionKey">Delegate that returns the entity partition key.</param>
        /// <param name="funcGetId">Delegate that returns the entity id.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{List{string}}"/>
        /// with information about each batch execution.</returns>
        public virtual List<AzCosmosResponse<List<string>>> GetEntitiesAsStringTransactionally<T>(
            IEnumerable<T> entities,
            Func<T, string> funcGetPartitionKey,
            Func<T, string> funcGetId,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return GetEntitiesAsStringTransactionallyAsync(entities, funcGetPartitionKey, funcGetId,
                cancellationToken, databaseId, containerId, partitionKeyPropName).WaitAndUnwrapException();
        }

        /// <summary>
        /// Gets entities in one or more batch transactions from the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="T">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{TransactionalBatchResponse}"/>
        /// with information about each batch execution.</returns>
        public virtual List<AzCosmosResponse<TransactionalBatchResponse>> GetEntitiesResponsesTransactionally<T>(
            IEnumerable<T> entities,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
            where T : BaseCosmosEntity
        {
            return GetEntitiesResponsesTransactionally(entities, entt => entt.PartitionKey, entt => entt.Id, 
                cancellationToken, databaseId, containerId, partitionKeyPropName);
        }

        /// <summary>
        /// Gets entities in one or more batch transactions from the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="funcGetPartitionKey">Delegate that returns the entity partition key.</param>
        /// <param name="funcGetId">Delegate that returns the entity id.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{TransactionalBatchResponse}"/>
        /// with information about each batch execution.</returns>
        public virtual List<AzCosmosResponse<TransactionalBatchResponse>> GetEntitiesResponsesTransactionally<T>(
            IEnumerable<T> entities,
            Func<T, string> funcGetPartitionKey,
            Func<T, string> funcGetId,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return GetEntityResponsesTransactionallyAsync(entities, funcGetPartitionKey, funcGetId,
                cancellationToken, databaseId, containerId, partitionKeyPropName).WaitAndUnwrapException();
        }

        #endregion

        #region Get transactionally async

        /// <summary>
        /// Gets entities in one or more batch transactions from the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="T">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="funcGetPartitionKey">Delegate that returns the entity partition key.</param>
        /// <param name="funcGetId">Delegate that returns the entity id.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{List{T}}"/>
        /// with information about each batch execution, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<List<AzCosmosResponse<List<T>>>> GetEntitiesTransactionallyAsync<T>(
            IEnumerable<T> entities,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
            where T : BaseCosmosEntity
        {
            return await GetEntitiesTransactionallyAsync(entities, entt => entt.PartitionKey, entt => entt.Id,
                cancellationToken, databaseId, containerId, partitionKeyPropName);
        }
        
        /// <summary>
        /// Gets entities in one or more batch transactions from the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="funcGetPartitionKey">Delegate that returns the entity partition key.</param>
        /// <param name="funcGetId">Delegate that returns the entity id.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{List{T}}"/>
        /// with information about each batch execution, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<List<AzCosmosResponse<List<T>>>> GetEntitiesTransactionallyAsync<T>(
            IEnumerable<T> entities,
            Func<T, string> funcGetPartitionKey,
            Func<T, string> funcGetId,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return (await GetEntitiesAsStringTransactionallyAsync(entities, funcGetPartitionKey, funcGetId,
                cancellationToken, databaseId, containerId, partitionKeyPropName))
                .DeserializeObjects<T>();
        }

        /// <summary>
        /// Gets entities as string in one or more batch transactions from the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="T">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{List{string}}"/>
        /// with information about each batch execution, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<List<AzCosmosResponse<List<string>>>> GetEntitiesAsStringTransactionallyAsync<T>(
            IEnumerable<T> entities,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
            where T : BaseCosmosEntity
        {
            return await GetEntitiesAsStringTransactionallyAsync(entities, entt => entt.PartitionKey, entt => entt.Id,
                cancellationToken, databaseId, containerId, partitionKeyPropName);
        }

        /// <summary>
        /// Gets entities as string in one or more batch transactions from the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="funcGetPartitionKey">Delegate that returns the entity partition key.</param>
        /// <param name="funcGetId">Delegate that returns the entity id.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{List{string}}"/>
        /// with information about each batch execution, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<List<AzCosmosResponse<List<string>>>> GetEntitiesAsStringTransactionallyAsync<T>(
            IEnumerable<T> entities,
            Func<T, string> funcGetPartitionKey,
            Func<T, string> funcGetId,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return (await GetEntityResponsesTransactionallyAsync(entities, funcGetPartitionKey, funcGetId,
                cancellationToken, databaseId, containerId, partitionKeyPropName)).InduceResponsesToValueStrings();
        }

        /// <summary>
        /// Gets entities in one or more batch transactions from the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="T">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{TransactionalBatchResponse}"/>
        /// with information about each batch execution, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<List<AzCosmosResponse<TransactionalBatchResponse>>> GetEntityResponsesTransactionallyAsync<T>(
            IEnumerable<T> entities,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
            where T : BaseCosmosEntity
        {
            return await GetEntityResponsesTransactionallyAsync(entities, entt => entt.PartitionKey, entt => entt.Id,
                cancellationToken, databaseId, containerId, partitionKeyPropName);
        }

        /// <summary>
        /// Gets entities in one or more batch transactions from the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="funcGetPartitionKey">Delegate that returns the entity partition key.</param>
        /// <param name="funcGetId">Delegate that returns the entity id.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{TransactionalBatchResponse}"/>
        /// with information about each batch execution, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<List<AzCosmosResponse<TransactionalBatchResponse>>> GetEntityResponsesTransactionallyAsync<T>(
            IEnumerable<T> entities,
            Func<T, string> funcGetPartitionKey,
            Func<T, string> funcGetId,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(funcGetId, nameof(funcGetId));

            return await ExecuteOperationTransactionallyAsync(entities,
                (azTableTransactionStore, entts) => azTableTransactionStore.ClearNGet(entts),
                funcGetPartitionKey, funcGetId, cancellationToken,
                databaseId, containerId, partitionKeyPropName);
        }

        #endregion

        #region Query

        #region QueryAll

        /// <summary>
        /// Queries all items of type <typeparamref name="T"/> under a container in an Azure Cosmos database.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="take">Amount of items to take.</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="AzCosmosResponse{List{T}}"/> containing a collection of items of type <typeparamref name="T"/>.</returns>
        public virtual AzCosmosResponse<List<T>> QueryAll<T>(
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            int take = AzCoreTools.Utilities.ConstProvider.DefaultTake,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            return QueryAll<T, AzCosmosResponse<List<T>>>(databaseId,
                containerId,
                partitionKeyPropName,
                take,
                continuationToken, 
                requestOptions,
                cancellationToken);
        }

        /// <summary>
        /// Queries all items of type <typeparamref name="T"/> under a container in an Azure Cosmos database.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{List{T}}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{List{T}}" />.</typeparam>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="take">Amount of items to take.</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="TOut"/> containing a collection of items of type <typeparamref name="T"/>.</returns>
        public virtual TOut QueryAll<T, TOut>(
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            int take = AzCoreTools.Utilities.ConstProvider.DefaultTake,
            string continuationToken = null, 
            QueryRequestOptions requestOptions = default, 
            CancellationToken cancellationToken = default)
            where TOut : AzCosmosResponse<List<T>>, new()
        {
            Initialize(databaseId, containerId, partitionKeyPropName, false, false);

            return CosmosFuncHelper.Execute<int, string, QueryRequestOptions, CancellationToken, AzCosmosResponse<List<T>>, TOut, List<T>>(
                Container.QueryAll<T>,
                take, continuationToken, requestOptions, cancellationToken);
        }

        #endregion

        #region LazyQueryAll

        /// <summary>
        /// Query allowing to iterate on demand over all items of type <typeparamref name="T"/> under a container in an Azure Cosmos database.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="AzCosmosResponse{IEnumerable{T}}"/> containing a collection 
        /// that allows to iterate on demand over items of type <typeparamref name="T"/>.</returns>
        public virtual AzCosmosResponse<IEnumerable<T>> LazyQueryAll<T>(
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            return LazyQueryAll<T, AzCosmosResponse<IEnumerable<T>>>(
                databaseId,
                containerId,
                partitionKeyPropName,
                continuationToken,
                requestOptions,
                cancellationToken);
        }

        /// <summary>
        /// Query allowing to iterate on demand over all items of type <typeparamref name="T"/> under a container in an Azure Cosmos database.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{IEnumerable{T}}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{IEnumerable{T}}" />.</typeparam>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="TOut"/> containing a collection that allows 
        /// to iterate on demand over items of type <typeparamref name="T"/>.</returns>
        public virtual TOut LazyQueryAll<T, TOut>(
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default) 
            where TOut : AzCosmosResponse<IEnumerable<T>>, new()
        {
            Initialize(databaseId, containerId, partitionKeyPropName, false, false);

            return CosmosFuncHelper.Execute<int, string, QueryRequestOptions, CancellationToken, AzCosmosResponse<IEnumerable<T>>, TOut, IEnumerable<T>>(
                Container.LazyQueryAll<T>,
                int.MaxValue, continuationToken, requestOptions, cancellationToken);
        }

        #endregion

        #region QueryByFilter

        /// <summary>
        /// Creates a query with the <paramref name="filter"/> for items of type <typeparamref name="T"/> 
        /// under a container in an Azure Cosmos database, using a SQL statement with parameterized values.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="filter">The Cosmos SQL query text.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="AzCosmosResponse{List{T}}"/> containing a collection of items of type <typeparamref name="T"/>.</returns>
        public virtual AzCosmosResponse<List<T>> QueryByFilter<T>(
            string filter,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            return QueryByFilter<T, AzCosmosResponse<List<T>>>(
                filter,
                databaseId,
                containerId,
                partitionKeyPropName,
                continuationToken,
                requestOptions,
                cancellationToken);
        }

        /// <summary>
        /// Creates a query with the <paramref name="filter"/> for items of type <typeparamref name="T"/> 
        /// under a container in an Azure Cosmos database, using a SQL statement with parameterized values.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{List{T}}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{List{T}}" />.</typeparam>
        /// <param name="filter">The Cosmos SQL query text.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="TOut"/> containing a collection of items of type <typeparamref name="T"/>.</returns>
        public virtual TOut QueryByFilter<T, TOut>(
            string filter,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default) 
            where TOut : AzCosmosResponse<List<T>>, new()
        {
            Initialize(databaseId, containerId, partitionKeyPropName, false, false);

            return CosmosFuncHelper.Execute<string, int, string, QueryRequestOptions, CancellationToken, AzCosmosResponse<List<T>>, TOut, List<T>>(
                Container.QueryByFilter<T>,
                filter, int.MaxValue, continuationToken, requestOptions, cancellationToken);
        }

        #endregion

        #region LazyQueryByFilter

        /// <summary>
        /// Creates a query with the <paramref name="filter"/> that allows to iterate on demand over 
        /// items of type <typeparamref name="T"/> under a container in an Azure Cosmos database, 
        /// using a SQL statement with parameterized values.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="filter">The Cosmos SQL query text.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="AzCosmosResponse{IEnumerable{T}}"/> containing a collection 
        /// that allows to iterate on demand over items of type <typeparamref name="T"/>.</returns>
        public virtual AzCosmosResponse<IEnumerable<T>> LazyQueryByFilter<T>(
            string filter,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            return LazyQueryByFilter<T, AzCosmosResponse<IEnumerable<T>>>(
                filter,
                databaseId,
                containerId,
                partitionKeyPropName,
                continuationToken,
                requestOptions,
                cancellationToken);
        }

        /// <summary>
        /// Creates a query with the <paramref name="filter"/> that allows to iterate on demand over 
        /// items of type <typeparamref name="T"/> under a container in an Azure Cosmos database, 
        /// using a SQL statement with parameterized values.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{IEnumerable{T}}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{IEnumerable{T}}" />.</typeparam>
        /// <param name="filter">The Cosmos SQL query text.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="TOut"/> containing a collection that allows 
        /// to iterate on demand over items of type <typeparamref name="T"/>.</returns>
        public virtual TOut LazyQueryByFilter<T, TOut>(
            string filter,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default) 
            where TOut : AzCosmosResponse<IEnumerable<T>>, new()
        {
            Initialize(databaseId, containerId, partitionKeyPropName, false, false);

            return CosmosFuncHelper.Execute<string, int, string, QueryRequestOptions, CancellationToken, AzCosmosResponse<IEnumerable<T>>, TOut, IEnumerable<T>>(
                Container.LazyQueryByFilter<T>,
                filter, int.MaxValue, continuationToken, requestOptions, cancellationToken);
        }

        #endregion

        #region QueryByQueryDefinition

        /// <summary>
        /// Creates a query with the <paramref name="queryDefinition"/> for items of type <typeparamref name="T"/> 
        /// under a container in an Azure Cosmos database, using a SQL statement with parameterized values.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="queryDefinition">The Cosmos SQL query definition.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="AzCosmosResponse{List{T}}"/> containing a collection of items of type <typeparamref name="T"/>.</returns>
        public virtual AzCosmosResponse<List<T>> QueryByQueryDefinition<T>(
            QueryDefinition queryDefinition,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            return QueryByQueryDefinition<T, AzCosmosResponse<List<T>>>(
                queryDefinition,
                databaseId,
                containerId,
                partitionKeyPropName,
                continuationToken,
                requestOptions,
                cancellationToken);
        }

        /// <summary>
        /// Creates a query with the <paramref name="queryDefinition"/> for items of type <typeparamref name="T"/> 
        /// under a container in an Azure Cosmos database, using a SQL statement with parameterized values.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{List{T}}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{List{T}}" />.</typeparam>
        /// <param name="queryDefinition">The Cosmos SQL query definition.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="TOut"/> containing a collection of items of type <typeparamref name="T"/>.</returns>
        public virtual TOut QueryByQueryDefinition<T, TOut>(
            QueryDefinition queryDefinition,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default) 
            where TOut : AzCosmosResponse<List<T>>, new()
        {
            Initialize(databaseId, containerId, partitionKeyPropName, false, false);

            return CosmosFuncHelper.Execute<QueryDefinition, int, string, QueryRequestOptions, CancellationToken, AzCosmosResponse<List<T>>, TOut, List<T>>(
                Container.QueryByQueryDefinition<T>,
                queryDefinition, int.MaxValue, continuationToken, requestOptions, cancellationToken);
        }

        #endregion

        #region LazyQueryByQueryDefinition

        /// <summary>
        /// Creates a query with the <paramref name="queryDefinition"/> that allows to iterate on demand over 
        /// items of type <typeparamref name="T"/> under a container in an Azure Cosmos database, 
        /// using a SQL statement with parameterized values.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="queryDefinition">The Cosmos SQL query definition.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="AzCosmosResponse{IEnumerable{T}}"/> containing a collection 
        /// that allows to iterate on demand over items of type <typeparamref name="T"/>.</returns>
        public virtual AzCosmosResponse<IEnumerable<T>> LazyQueryByQueryDefinition<T>(
            QueryDefinition queryDefinition,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            return LazyQueryByQueryDefinition<T, AzCosmosResponse<IEnumerable<T>>>(
                queryDefinition,
                databaseId,
                containerId,
                partitionKeyPropName,
                continuationToken,
                requestOptions,
                cancellationToken);
        }

        /// <summary>
        /// Creates a query with the <paramref name="queryDefinition"/> that allows to iterate on demand over 
        /// items of type <typeparamref name="T"/> under a container in an Azure Cosmos database, 
        /// using a SQL statement with parameterized values.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{IEnumerable{T}}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{IEnumerable{T}}" />.</typeparam>
        /// <param name="queryDefinition">The Cosmos SQL query definition.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="TOut"/> containing a collection that allows 
        /// to iterate on demand over items of type <typeparamref name="T"/>.</returns>
        public virtual TOut LazyQueryByQueryDefinition<T, TOut>(
            QueryDefinition queryDefinition,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default) 
            where TOut : AzCosmosResponse<IEnumerable<T>>, new()
        {
            Initialize(databaseId, containerId, partitionKeyPropName, false, false);

            return CosmosFuncHelper.Execute<QueryDefinition, int, string, QueryRequestOptions, CancellationToken, AzCosmosResponse<IEnumerable<T>>, TOut, IEnumerable<T>>(
                Container.LazyQueryByQueryDefinition<T>,
                queryDefinition, int.MaxValue, continuationToken, requestOptions, cancellationToken);
        }

        #endregion

        #region QueryByPartitionKey

        /// <summary>
        /// Queries items of type <typeparamref name="T"/> with the specified partition key value under a container in an Azure Cosmos database.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="partitionKey">The partition key value of the item.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="AzCosmosResponse{List{T}}"/> containing a collection of items of type <typeparamref name="T"/>.</returns>
        public virtual AzCosmosResponse<List<T>> QueryByPartitionKey<T>(
            string partitionKey,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            return QueryByPartitionKey<T, AzCosmosResponse<List<T>>>(
                partitionKey,
                databaseId,
                containerId,
                partitionKeyPropName,
                continuationToken,
                requestOptions,
                cancellationToken);
        }

        /// <summary>
        /// Queries items of type <typeparamref name="T"/> with the specified partition key value under a container in an Azure Cosmos database.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{List{T}}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{List{T}}" />.</typeparam>
        /// <param name="partitionKey">The partition key value of the item.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="TOut"/> containing a collection of items of type <typeparamref name="T"/>.</returns>
        public virtual TOut QueryByPartitionKey<T, TOut>(
            string partitionKey,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
            where TOut : AzCosmosResponse<List<T>>, new()
        {
            Initialize(databaseId, containerId, partitionKeyPropName, false, false);

            return CosmosFuncHelper.Execute<string, int, string, QueryRequestOptions, CancellationToken, AzCosmosResponse<List<T>>, TOut, List<T>>(
                Container.QueryByPartitionKey<T>,
                partitionKey, int.MaxValue, continuationToken, requestOptions, cancellationToken);
        }

        #endregion

        #region LazyQueryByPartitionKey

        /// <summary>
        /// Query allowing to iterate on demand over items of type <typeparamref name="T"/> 
        /// with the specified partition key value, under a container in an Azure Cosmos database
        /// using a SQL statement with parameterized values.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="partitionKey">The partition key value of the item.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="AzCosmosResponse{IEnumerable{T}}"/> containing a collection 
        /// that allows to iterate on demand over items of type <typeparamref name="T"/>.</returns>
        public virtual AzCosmosResponse<IEnumerable<T>> LazyQueryByPartitionKey<T>(
            string partitionKey,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            return LazyQueryByPartitionKey<T, AzCosmosResponse<IEnumerable<T>>>(
                partitionKey,
                databaseId,
                containerId,
                partitionKeyPropName,
                continuationToken,
                requestOptions,
                cancellationToken);
        }

        /// <summary>
        /// Query allowing to iterate on demand over items of type <typeparamref name="T"/> 
        /// with the specified partition key value, under a container in an Azure Cosmos database
        /// using a SQL statement with parameterized values.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{IEnumerable{T}}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{IEnumerable{T}}" />.</typeparam>
        /// <param name="partitionKey">The partition key value of the item.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="TOut"/> containing a collection that allows 
        /// to iterate on demand over items of type <typeparamref name="T"/>.</returns>
        public virtual TOut LazyQueryByPartitionKey<T, TOut>(
            string partitionKey,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default) 
            where TOut : AzCosmosResponse<IEnumerable<T>>, new()
        {
            Initialize(databaseId, containerId, partitionKeyPropName, false, false);

            return CosmosFuncHelper.Execute<string, int, string, QueryRequestOptions, CancellationToken, AzCosmosResponse<IEnumerable<T>>, TOut, IEnumerable<T>>(
                Container.LazyQueryByPartitionKey<T>,
                partitionKey, int.MaxValue, continuationToken, requestOptions, cancellationToken);
        }

        #endregion

        #region QueryWithOr

        /// <summary>
        /// Queries items of type <typeparamref name="T"/>, creating a SQL statement query which compares with logical 'or' 
        /// using properties (names-values) contained in <paramref name="nameValueProperties"/>, under a container in an Azure Cosmos database.
        /// Property names must match the properties on type <typeparamref name="T"/> serialized by Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="nameValueProperties">Property names and values to include in SQL query.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="AzCosmosResponse{List{T}}"/> containing a collection of items of type <typeparamref name="T"/>.</returns>
        public virtual AzCosmosResponse<List<T>> QueryWithOr<T>(
            IEnumerable<KeyValuePair<string, string>> nameValueProperties,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            return QueryWithOr<T, AzCosmosResponse<List<T>>>(
                nameValueProperties,
                databaseId,
                containerId,
                partitionKeyPropName,
                continuationToken,
                requestOptions,
                cancellationToken);
        }

        /// <summary>
        /// Queries items of type <typeparamref name="T"/>, creating a SQL statement query which compares with logical 'or' 
        /// using properties (names-values) contained in <paramref name="nameValueProperties"/>, under a container in an Azure Cosmos database.
        /// Property names must match the properties on type <typeparamref name="T"/> serialized by Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{List{T}}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{List{T}}" />.</typeparam>
        /// <param name="nameValueProperties">Property names and values to include in SQL query.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="TOut"/> containing a collection of items of type <typeparamref name="T"/>.</returns>
        public virtual TOut QueryWithOr<T, TOut>(
            IEnumerable<KeyValuePair<string, string>> nameValueProperties,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default) 
            where TOut : AzCosmosResponse<List<T>>, new()
        {
            ThrowIfInvalidNameValueProperties(nameValueProperties);

            return QueryByQueryDefinition<T, TOut>(GenerateQueryDefinition(nameValueProperties, BooleanOperator.or),
                databaseId, containerId, partitionKeyPropName, continuationToken, requestOptions, cancellationToken);
        }

        /// <summary>
        /// Queries items of type <typeparamref name="T"/>, creating a SQL statement query which compares with logical 'or' 
        /// using properties (names-values) of <paramref name="operationTerms"/>, under a container in an Azure Cosmos database.
        /// Property names must match the properties on type <typeparamref name="T"/> serialized by Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="operationTerms">Contains property names and values to include in SQL query.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="AzCosmosResponse{List{T}}"/> containing a collection of items of type <typeparamref name="T"/>.</returns>
        public virtual AzCosmosResponse<List<T>> QueryWithOr<T>(
            dynamic operationTerms,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            return QueryWithOr<T, AzCosmosResponse<List<T>>>(
                operationTerms,
                databaseId,
                containerId,
                partitionKeyPropName,
                continuationToken,
                requestOptions,
                cancellationToken);
        }

        /// <summary>
        /// Queries items of type <typeparamref name="T"/>, creating a SQL statement query which compares with logical 'or' 
        /// using properties (names-values) of <paramref name="operationTerms"/>, under a container in an Azure Cosmos database.
        /// Property names must match the properties on type <typeparamref name="T"/> serialized by Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{List{T}}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{List{T}}" />.</typeparam>
        /// <param name="operationTerms">Contains property names and values to include in SQL query.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="TOut"/> containing a collection of items of type <typeparamref name="T"/>.</returns>
        public virtual TOut QueryWithOr<T, TOut>(
            dynamic operationTerms,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default) 
            where TOut : AzCosmosResponse<List<T>>, new()
        {
            ExThrower.ST_ThrowIfArgumentIsNull(operationTerms, nameof(operationTerms), nameof(operationTerms));

            return QueryByQueryDefinition<T, TOut>(GenerateQueryDefinition(operationTerms, BooleanOperator.or), 
                databaseId, containerId, partitionKeyPropName, continuationToken, requestOptions, cancellationToken);
        }

        #endregion

        #region LazyQueryWithOr

        /// <summary>
        /// Query allowing to iterate on demand over items of type <typeparamref name="T"/>, 
        /// creating a SQL statement query which compares with logical 'or' 
        /// using properties (names-values) contained in <paramref name="nameValueProperties"/>,
        /// under a container in an Azure Cosmos database.
        /// Property names must match the properties on type <typeparamref name="T"/> serialized by Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="nameValueProperties">Property names and values to include in SQL query.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="AzCosmosResponse{IEnumerable{T}}"/> containing a collection 
        /// that allows to iterate on demand over items of type <typeparamref name="T"/>.</returns>
        public virtual AzCosmosResponse<IEnumerable<T>> LazyQueryWithOr<T>(
            IEnumerable<KeyValuePair<string, string>> nameValueProperties,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            return LazyQueryWithOr<T, AzCosmosResponse<IEnumerable<T>>>(
                nameValueProperties,
                databaseId,
                containerId,
                partitionKeyPropName,
                continuationToken,
                requestOptions,
                cancellationToken);
        }

        /// <summary>
        /// Query allowing to iterate on demand over items of type <typeparamref name="T"/>, 
        /// creating a SQL statement query which compares with logical 'or' 
        /// using properties (names-values) contained in <paramref name="nameValueProperties"/>,
        /// under a container in an Azure Cosmos database.
        /// Property names must match the properties on type <typeparamref name="T"/> serialized by Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{IEnumerable{T}}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{IEnumerable{T}}" />.</typeparam>
        /// <param name="nameValueProperties">Property names and values to include in SQL query.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="TOut"/> containing a collection that allows 
        /// to iterate on demand over items of type <typeparamref name="T"/>.</returns>
        public virtual TOut LazyQueryWithOr<T, TOut>(
            IEnumerable<KeyValuePair<string, string>> nameValueProperties,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default) 
            where TOut : AzCosmosResponse<IEnumerable<T>>, new()
        {
            ThrowIfInvalidNameValueProperties(nameValueProperties);

            return LazyQueryByQueryDefinition<T, TOut>(GenerateQueryDefinition(nameValueProperties, BooleanOperator.or),
                databaseId, containerId, partitionKeyPropName, continuationToken, requestOptions, cancellationToken);
        }

        /// <summary>
        /// Query allowing to iterate on demand over items of type <typeparamref name="T"/>, 
        /// creating a SQL statement query which compares with logical 'or' 
        /// using properties (names-values) of <paramref name="operationTerms"/>,
        /// under a container in an Azure Cosmos database.
        /// Property names must match the properties on type <typeparamref name="T"/> serialized by Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="operationTerms">Contains property names and values to include in SQL query.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="AzCosmosResponse{IEnumerable{T}}"/> containing a collection 
        /// that allows to iterate on demand over items of type <typeparamref name="T"/>.</returns>
        public virtual AzCosmosResponse<IEnumerable<T>> LazyQueryWithOr<T>(
            dynamic operationTerms,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            return LazyQueryWithOr<T, AzCosmosResponse<IEnumerable<T>>>(
                operationTerms,
                databaseId,
                containerId,
                partitionKeyPropName,
                continuationToken,
                requestOptions,
                cancellationToken);
        }

        /// <summary>
        /// Query allowing to iterate on demand over items of type <typeparamref name="T"/>, 
        /// creating a SQL statement query which compares with logical 'or' 
        /// using properties (names-values) of <paramref name="operationTerms"/>,
        /// under a container in an Azure Cosmos database.
        /// Property names must match the properties on type <typeparamref name="T"/> serialized by Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{IEnumerable{T}}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{IEnumerable{T}}" />.</typeparam>
        /// <param name="operationTerms">Contains property names and values to include in SQL query.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="TOut"/> containing a collection that allows 
        /// to iterate on demand over items of type <typeparamref name="T"/>.</returns>
        public virtual TOut LazyQueryWithOr<T, TOut>(
            dynamic operationTerms,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default) 
            where TOut : AzCosmosResponse<IEnumerable<T>>, new()
        {
            ExThrower.ST_ThrowIfArgumentIsNull(operationTerms, nameof(operationTerms), nameof(operationTerms));

            return LazyQueryByQueryDefinition<T, TOut>(GenerateQueryDefinition(operationTerms, BooleanOperator.or), 
                databaseId, containerId, partitionKeyPropName, continuationToken, requestOptions, cancellationToken);
        }

        #endregion

        #region QueryWithAnd

        /// <summary>
        /// Queries items of type <typeparamref name="T"/>, creating a SQL statement query which compares with logical 'and' 
        /// using properties (names-values) contained in <paramref name="nameValueProperties"/>, under a container in an Azure Cosmos database.
        /// Property names must match the properties on type <typeparamref name="T"/> serialized by Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="nameValueProperties">Property names and values to include in SQL query.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="AzCosmosResponse{List{T}}"/> containing a collection of items of type <typeparamref name="T"/>.</returns>
        public virtual AzCosmosResponse<List<T>> QueryWithAnd<T>(
            IEnumerable<KeyValuePair<string, string>> nameValueProperties,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            return QueryWithAnd<T, AzCosmosResponse<List<T>>>(
                nameValueProperties,
                databaseId,
                containerId,
                partitionKeyPropName,
                continuationToken,
                requestOptions,
                cancellationToken);
        }

        /// <summary>
        /// Queries items of type <typeparamref name="T"/>, creating a SQL statement query which compares with logical 'and' 
        /// using properties (names-values) contained in <paramref name="nameValueProperties"/>, under a container in an Azure Cosmos database.
        /// Property names must match the properties on type <typeparamref name="T"/> serialized by Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{List{T}}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{List{T}}" />.</typeparam>
        /// <param name="nameValueProperties">Property names and values to include in SQL query.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="TOut"/> containing a collection of items of type <typeparamref name="T"/>.</returns>
        public virtual TOut QueryWithAnd<T, TOut>(
            IEnumerable<KeyValuePair<string, string>> nameValueProperties,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default) 
            where TOut : AzCosmosResponse<List<T>>, new()
        {
            ThrowIfInvalidNameValueProperties(nameValueProperties);

            return QueryByQueryDefinition<T, TOut>(GenerateQueryDefinition(nameValueProperties, BooleanOperator.and),
                databaseId, containerId, partitionKeyPropName, continuationToken, requestOptions, cancellationToken);
        }

        /// <summary>
        /// Queries items of type <typeparamref name="T"/>, creating a SQL statement query which compares with logical 'and' 
        /// using properties (names-values) of <paramref name="operationTerms"/>, under a container in an Azure Cosmos database.
        /// Property names must match the properties on type <typeparamref name="T"/> serialized by Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="operationTerms">Contains property names and values to include in SQL query.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="AzCosmosResponse{List{T}}"/> containing a collection of items of type <typeparamref name="T"/>.</returns>
        public virtual AzCosmosResponse<List<T>> QueryWithAnd<T>(
            dynamic operationTerms,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            return QueryWithAnd<T, AzCosmosResponse<List<T>>>(
                operationTerms,
                databaseId,
                containerId,
                partitionKeyPropName,
                continuationToken,
                requestOptions,
                cancellationToken);
        }

        /// <summary>
        /// Queries items of type <typeparamref name="T"/>, creating a SQL statement query which compares with logical 'and' 
        /// using properties (names-values) of <paramref name="operationTerms"/>, under a container in an Azure Cosmos database.
        /// Property names must match the properties on type <typeparamref name="T"/> serialized by Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{List{T}}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{List{T}}" />.</typeparam>
        /// <param name="operationTerms">Contains property names and values to include in SQL query.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="TOut"/> containing a collection of items of type <typeparamref name="T"/>.</returns>
        public virtual TOut QueryWithAnd<T, TOut>(
            dynamic operationTerms,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default) 
            where TOut : AzCosmosResponse<List<T>>, new()
        {
            ExThrower.ST_ThrowIfArgumentIsNull(operationTerms, nameof(operationTerms), nameof(operationTerms));

            return QueryByQueryDefinition<T, TOut>(GenerateQueryDefinition(operationTerms, BooleanOperator.and), 
                databaseId, containerId, partitionKeyPropName, continuationToken, requestOptions, cancellationToken);
        }

        #endregion

        #region LazyQueryWithAnd

        /// <summary>
        /// Query allowing to iterate on demand over items of type <typeparamref name="T"/>, 
        /// creating a SQL statement query which compares with logical 'and' 
        /// using properties (names-values) contained in <paramref name="nameValueProperties"/>,
        /// under a container in an Azure Cosmos database.
        /// Property names must match the properties on type <typeparamref name="T"/> serialized by Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="nameValueProperties">Property names and values to include in SQL query.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A <see cref="AzCosmosResponse{IEnumerable{T}}"/> containing a collection 
        /// that allows to iterate on demand over items of type <typeparamref name="T"/>.</returns>
        public virtual AzCosmosResponse<IEnumerable<T>> LazyQueryWithAnd<T>(
            IEnumerable<KeyValuePair<string, string>> nameValueProperties,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            return LazyQueryWithAnd<T, AzCosmosResponse<IEnumerable<T>>>(
                nameValueProperties,
                databaseId,
                containerId,
                partitionKeyPropName,
                continuationToken,
                requestOptions,
                cancellationToken);
        }

        /// <summary>
        /// Query allowing to iterate on demand over items of type <typeparamref name="T"/>, 
        /// creating a SQL statement query which compares with logical 'and' 
        /// using properties (names-values) contained in <paramref name="nameValueProperties"/>,
        /// under a container in an Azure Cosmos database.
        /// Property names must match the properties on type <typeparamref name="T"/> serialized by Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{IEnumerable{T}}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{IEnumerable{T}}" />.</typeparam>
        /// <param name="nameValueProperties">Property names and values to include in SQL query.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A <see cref="TOut"/> containing a collection that allows 
        /// to iterate on demand over items of type <typeparamref name="T"/>.</returns>
        public virtual TOut LazyQueryWithAnd<T, TOut>(
            IEnumerable<KeyValuePair<string, string>> nameValueProperties,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default) 
            where TOut : AzCosmosResponse<IEnumerable<T>>, new()
        {
            ThrowIfInvalidNameValueProperties(nameValueProperties);

            return LazyQueryByQueryDefinition<T, TOut>(GenerateQueryDefinition(nameValueProperties, BooleanOperator.and),
                databaseId, containerId, partitionKeyPropName, continuationToken, requestOptions, cancellationToken);
        }

        /// <summary>
        /// Query allowing to iterate on demand over items of type <typeparamref name="T"/>, 
        /// creating a SQL statement query which compares with logical 'and' 
        /// using properties (names-values) of <paramref name="operationTerms"/>,
        /// under a container in an Azure Cosmos database.
        /// Property names must match the properties on type <typeparamref name="T"/> serialized by Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="operationTerms">Contains property names and values to include in SQL query.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A <see cref="AzCosmosResponse{IEnumerable{T}}"/> containing a collection 
        /// that allows to iterate on demand over items of type <typeparamref name="T"/>.</returns>
        public virtual AzCosmosResponse<IEnumerable<T>> LazyQueryWithAnd<T>(
            dynamic operationTerms,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            return LazyQueryWithAnd<T, AzCosmosResponse<IEnumerable<T>>>(
                operationTerms,
                databaseId,
                containerId,
                partitionKeyPropName,
                continuationToken,
                requestOptions,
                cancellationToken);
        }

        /// <summary>
        /// Query allowing to iterate on demand over items of type <typeparamref name="T"/>, 
        /// creating a SQL statement query which compares with logical 'and' 
        /// using properties (names-values) of <paramref name="operationTerms"/>,
        /// under a container in an Azure Cosmos database.
        /// Property names must match the properties on type <typeparamref name="T"/> serialized by Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{IEnumerable{T}}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{IEnumerable{T}}" />.</typeparam>
        /// <param name="operationTerms">Contains property names and values to include in SQL query.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A <see cref="TOut"/> containing a collection that allows 
        /// to iterate on demand over items of type <typeparamref name="T"/>.</returns>
        public virtual TOut LazyQueryWithAnd<T, TOut>(
            dynamic operationTerms,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default) 
            where TOut : AzCosmosResponse<IEnumerable<T>>, new()
        {
            ExThrower.ST_ThrowIfArgumentIsNull(operationTerms, nameof(operationTerms), nameof(operationTerms));

            return LazyQueryByQueryDefinition<T, TOut>(GenerateQueryDefinition(operationTerms, BooleanOperator.and), 
                databaseId, containerId, partitionKeyPropName, continuationToken, requestOptions, cancellationToken);
        }

        #endregion

        #endregion

        #region Query async

        #region QueryAllAsync

        /// <summary>
        /// Queries all items of type <typeparamref name="T"/> under a container in an Azure Cosmos database.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="AzCosmosResponse{List{T}}"/> containing a collection of items of type <typeparamref name="T"/>, 
        /// that was created contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.
        /// </returns>
        public virtual async Task<AzCosmosResponse<List<T>>> QueryAllAsync<T>(
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            return await QueryAllAsync<T, AzCosmosResponse<List<T>>>(databaseId,
                containerId,
                partitionKeyPropName,
                continuationToken,
                requestOptions,
                cancellationToken);
        }

        /// <summary>
        /// Queries all items of type <typeparamref name="T"/> under a container in an Azure Cosmos database.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{List{T}}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{List{T}}" />.</typeparam>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="TOut"/> containing a collection of items of type <typeparamref name="T"/>, 
        /// that was created contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.
        /// </returns>
        public virtual async Task<TOut> QueryAllAsync<T, TOut>(
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default) 
            where TOut : AzCosmosResponse<List<T>>, new()
        {
            Initialize(databaseId, containerId, partitionKeyPropName, false, false);

            return await CosmosFuncHelper.ExecuteAsync<int, string, QueryRequestOptions, CancellationToken, AzCosmosResponse<List<T>>, TOut, List<T>>(
                Container.QueryAllAsync<T>,
                int.MaxValue, continuationToken, requestOptions, cancellationToken);
        }

        #endregion

        #region QueryByFilterAsync

        /// <summary>
        /// Creates a query with the <paramref name="filter"/> for items of type <typeparamref name="T"/> 
        /// under a container in an Azure Cosmos database, using a SQL statement with parameterized values.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="filter">The Cosmos SQL query text.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="AzCosmosResponse{List{T}}"/> containing a collection of items of type <typeparamref name="T"/>, 
        /// that was created contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.</returns>
        public virtual async Task<AzCosmosResponse<List<T>>> QueryByFilterAsync<T>(
            string filter,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            return await QueryByFilterAsync<T, AzCosmosResponse<List<T>>>(
                filter,
                databaseId,
                containerId,
                partitionKeyPropName,
                continuationToken,
                requestOptions,
                cancellationToken);
        }

        /// <summary>
        /// Creates a query with the <paramref name="filter"/> for items of type <typeparamref name="T"/> 
        /// under a container in an Azure Cosmos database, using a SQL statement with parameterized values.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{List{T}}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{List{T}}" />.</typeparam>
        /// <param name="filter">The Cosmos SQL query text.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="TOut"/> containing a collection of items of type <typeparamref name="T"/>, 
        /// that was created contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.</returns>
        public virtual async Task<TOut> QueryByFilterAsync<T, TOut>(
            string filter,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default) 
            where TOut : AzCosmosResponse<List<T>>, new()
        {
            Initialize(databaseId, containerId, partitionKeyPropName, false, false);

            return await CosmosFuncHelper.ExecuteAsync<string, int, string, QueryRequestOptions, CancellationToken, AzCosmosResponse<List<T>>, TOut, List<T>>(
                Container.QueryByFilterAsync<T>,
                filter, int.MaxValue, continuationToken, requestOptions, cancellationToken);
        }

        #endregion

        #region QueryByQueryDefinitionAsync

        /// <summary>
        /// Creates a query with the <paramref name="queryDefinition"/> for items of type <typeparamref name="T"/> 
        /// under a container in an Azure Cosmos database, using a SQL statement with parameterized values.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="queryDefinition">The Cosmos SQL query definition.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="AzCosmosResponse{List{T}}"/> containing a collection of items of type <typeparamref name="T"/>.</returns>
        public virtual async Task<AzCosmosResponse<List<T>>> QueryByQueryDefinitionAsync<T>(
            QueryDefinition queryDefinition,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            return await QueryByQueryDefinitionAsync<T, AzCosmosResponse<List<T>>>(
                queryDefinition,
                databaseId,
                containerId,
                partitionKeyPropName,
                continuationToken,
                requestOptions,
                cancellationToken);
        }

        /// <summary>
        /// Creates a query with the <paramref name="queryDefinition"/> for items of type <typeparamref name="T"/> 
        /// under a container in an Azure Cosmos database, using a SQL statement with parameterized values.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{List{T}}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{List{T}}" />.</typeparam>
        /// <param name="queryDefinition">The Cosmos SQL query definition.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="TOut"/> containing a collection of items of type <typeparamref name="T"/>, 
        /// that was created contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.</returns>
        public virtual async Task<TOut> QueryByQueryDefinitionAsync<T, TOut>(
            QueryDefinition queryDefinition,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default) 
            where TOut : AzCosmosResponse<List<T>>, new()
        {
            Initialize(databaseId, containerId, partitionKeyPropName, false, false);

            return await CosmosFuncHelper.ExecuteAsync<QueryDefinition, int, string, QueryRequestOptions, CancellationToken, AzCosmosResponse<List<T>>, TOut, List<T>>(
                Container.QueryByQueryDefinitionAsync<T>,
                queryDefinition, int.MaxValue, continuationToken, requestOptions, cancellationToken);
        }

        #endregion

        #region QueryByPartitionKeyAsync

        /// <summary>
        /// Queries items of type <typeparamref name="T"/> with the specified partition key value under a container in an Azure Cosmos database.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="partitionKey">The partition key value of the item.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="AzCosmosResponse{List{T}}"/> containing a collection of items of type <typeparamref name="T"/>, 
        /// that was created contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.</returns>
        public virtual async Task<AzCosmosResponse<List<T>>> QueryByPartitionKeyAsync<T>(
            string partitionKey,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            return await QueryByPartitionKeyAsync<T, AzCosmosResponse<List<T>>>(
                partitionKey,
                databaseId,
                containerId,
                partitionKeyPropName,
                continuationToken,
                requestOptions,
                cancellationToken);
        }

        /// <summary>
        /// Queries items of type <typeparamref name="T"/> with the specified partition key value under a container in an Azure Cosmos database.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{List{T}}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{List{T}}" />.</typeparam>
        /// <param name="partitionKey">The partition key value of the item.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="TOut"/> containing a collection of items of type <typeparamref name="T"/>, 
        /// that was created contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.</returns>
        public virtual async Task<TOut> QueryByPartitionKeyAsync<T, TOut>(
            string partitionKey,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default) 
            where TOut : AzCosmosResponse<List<T>>, new()
        {
            Initialize(databaseId, containerId, partitionKeyPropName, false, false);

            return await CosmosFuncHelper.ExecuteAsync<string, int, string, QueryRequestOptions, CancellationToken, AzCosmosResponse<List<T>>, TOut, List<T>>(
                Container.QueryByPartitionKeyAsync<T>,
                partitionKey, int.MaxValue, continuationToken, requestOptions, cancellationToken);
        }

        #endregion

        #region QueryWithOrAsync

        /// <summary>
        /// Queries items of type <typeparamref name="T"/>, creating a SQL statement query which compares with logical 'or' 
        /// using properties (names-values) contained in <paramref name="nameValueProperties"/>, under a container in an Azure Cosmos database.
        /// Property names must match the properties on type <typeparamref name="T"/> serialized by Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="nameValueProperties">Property names and values to include in SQL query.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="AzCosmosResponse{List{T}}"/> containing a collection of items of type <typeparamref name="T"/>, 
        /// that was created contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.</returns>
        public virtual async Task<AzCosmosResponse<List<T>>> QueryWithOrAsync<T>(
            IEnumerable<KeyValuePair<string, string>> nameValueProperties,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            return await QueryWithOrAsync<T, AzCosmosResponse<List<T>>>(
                nameValueProperties,
                databaseId,
                containerId,
                partitionKeyPropName,
                continuationToken,
                requestOptions,
                cancellationToken);
        }

        /// <summary>
        /// Queries items of type <typeparamref name="T"/>, creating a SQL statement query which compares with logical 'or' 
        /// using properties (names-values) contained in <paramref name="nameValueProperties"/>, under a container in an Azure Cosmos database.
        /// Property names must match the properties on type <typeparamref name="T"/> serialized by Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{List{T}}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{List{T}}" />.</typeparam>
        /// <param name="nameValueProperties">Property names and values to include in SQL query.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="TOut"/> containing a collection of items of type <typeparamref name="T"/>, 
        /// that was created contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.</returns>
        public virtual async Task<TOut> QueryWithOrAsync<T, TOut>(
            IEnumerable<KeyValuePair<string, string>> nameValueProperties,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default) 
            where TOut : AzCosmosResponse<List<T>>, new()
        {
            ThrowIfInvalidNameValueProperties(nameValueProperties);

            return await QueryByQueryDefinitionAsync<T, TOut>(GenerateQueryDefinition(nameValueProperties, BooleanOperator.or),
                databaseId, containerId, partitionKeyPropName, continuationToken, requestOptions, cancellationToken);
        }

        /// <summary>
        /// Queries items of type <typeparamref name="T"/>, creating a SQL statement query which compares with logical 'or' 
        /// using properties (names-values) of <paramref name="operationTerms"/>, under a container in an Azure Cosmos database.
        /// Property names must match the properties on type <typeparamref name="T"/> serialized by Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="operationTerms">Contains property names and values to include in SQL query.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="AzCosmosResponse{List{T}}"/> containing a collection of items of type <typeparamref name="T"/>, 
        /// that was created contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.</returns>
        public virtual async Task<AzCosmosResponse<List<T>>> QueryWithOrAsync<T>(
            dynamic operationTerms,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            return await QueryWithOrAsync<T, AzCosmosResponse<List<T>>>(
                operationTerms,
                databaseId,
                containerId,
                partitionKeyPropName,
                continuationToken,
                requestOptions,
                cancellationToken);
        }

        /// <summary>
        /// Queries items of type <typeparamref name="T"/>, creating a SQL statement query which compares with logical 'or' 
        /// using properties (names-values) of <paramref name="operationTerms"/>, under a container in an Azure Cosmos database.
        /// Property names must match the properties on type <typeparamref name="T"/> serialized by Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{List{T}}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{List{T}}" />.</typeparam>
        /// <param name="operationTerms">Contains property names and values to include in SQL query.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="TOut"/> containing a collection of items of type <typeparamref name="T"/>, 
        /// that was created contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.</returns>
        public virtual async Task<TOut> QueryWithOrAsync<T, TOut>(
            dynamic operationTerms,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default) 
            where TOut : AzCosmosResponse<List<T>>, new()
        {
            ExThrower.ST_ThrowIfArgumentIsNull(operationTerms, nameof(operationTerms), nameof(operationTerms));

            return await QueryByQueryDefinitionAsync<T, TOut>(GenerateQueryDefinition(operationTerms, BooleanOperator.or),
                databaseId, containerId, partitionKeyPropName, continuationToken, requestOptions, cancellationToken);
        }

        #endregion

        #region QueryWithAndAsync

        /// <summary>
        /// Queries items of type <typeparamref name="T"/>, creating a SQL statement query which compares with logical 'and' 
        /// using properties (names-values) contained in <paramref name="nameValueProperties"/>, under a container in an Azure Cosmos database.
        /// Property names must match the properties on type <typeparamref name="T"/> serialized by Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="nameValueProperties">Property names and values to include in SQL query.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="AzCosmosResponse{List{T}}"/> containing a collection of items of type <typeparamref name="T"/>, 
        /// that was created contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.</returns>
        public virtual async Task<AzCosmosResponse<List<T>>> QueryWithAndAsync<T>(
            IEnumerable<KeyValuePair<string, string>> nameValueProperties,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            return await QueryWithAndAsync<T, AzCosmosResponse<List<T>>>(
                nameValueProperties,
                databaseId,
                containerId,
                partitionKeyPropName,
                continuationToken,
                requestOptions,
                cancellationToken);
        }

        /// <summary>
        /// Queries items of type <typeparamref name="T"/>, creating a SQL statement query which compares with logical 'and' 
        /// using properties (names-values) contained in <paramref name="nameValueProperties"/>, under a container in an Azure Cosmos database.
        /// Property names must match the properties on type <typeparamref name="T"/> serialized by Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{List{T}}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{List{T}}" />.</typeparam>
        /// <param name="nameValueProperties">Property names and values to include in SQL query.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="TOut"/> containing a collection of items of type <typeparamref name="T"/>, 
        /// that was created contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.</returns>
        public virtual async Task<TOut> QueryWithAndAsync<T, TOut>(
            IEnumerable<KeyValuePair<string, string>> nameValueProperties,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default) 
            where TOut : AzCosmosResponse<List<T>>, new()
        {
            ThrowIfInvalidNameValueProperties(nameValueProperties);

            return await QueryByQueryDefinitionAsync<T, TOut>(GenerateQueryDefinition(nameValueProperties, BooleanOperator.and),
                databaseId, containerId, partitionKeyPropName, continuationToken, requestOptions, cancellationToken);
        }

        /// <summary>
        /// Queries items of type <typeparamref name="T"/>, creating a SQL statement query which compares with logical 'and' 
        /// using properties (names-values) of <paramref name="operationTerms"/>, under a container in an Azure Cosmos database.
        /// Property names must match the properties on type <typeparamref name="T"/> serialized by Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="operationTerms">Contains property names and values to include in SQL query.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="AzCosmosResponse{List{T}}"/> containing a collection of items of type <typeparamref name="T"/>, 
        /// that was created contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.</returns>
        public virtual async Task<AzCosmosResponse<List<T>>> QueryWithAndAsync<T>(
            dynamic operationTerms,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            return await QueryWithAndAsync<T, AzCosmosResponse<List<T>>>(
                operationTerms,
                databaseId,
                containerId,
                partitionKeyPropName,
                continuationToken,
                requestOptions,
                cancellationToken);
        }

        /// <summary>
        /// Queries items of type <typeparamref name="T"/>, creating a SQL statement query which compares with logical 'and' 
        /// using properties (names-values) of <paramref name="operationTerms"/>, under a container in an Azure Cosmos database.
        /// Property names must match the properties on type <typeparamref name="T"/> serialized by Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{List{T}}" /> 
        /// or a custom model type that inherits from <see cref="AzCosmosResponse{List{T}}" />.</typeparam>
        /// <param name="operationTerms">Contains property names and values to include in SQL query.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <param name="continuationToken">The continuation token in the Azure Cosmos DB service.</param>
        /// <param name="requestOptions">The options for the item query request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A <see cref="TOut"/> containing a collection of items of type <typeparamref name="T"/>, 
        /// that was created contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.</returns>
        public virtual async Task<TOut> QueryWithAndAsync<T, TOut>(
            dynamic operationTerms,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null,
            string continuationToken = null,
            QueryRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default) 
            where TOut : AzCosmosResponse<List<T>>, new()
        {
            ExThrower.ST_ThrowIfArgumentIsNull(operationTerms, nameof(operationTerms), nameof(operationTerms));

            return await QueryByQueryDefinitionAsync<T, TOut>(GenerateQueryDefinition(operationTerms, BooleanOperator.and),
                databaseId, containerId, partitionKeyPropName, continuationToken, requestOptions, cancellationToken);
        }

        #endregion

        #endregion

        #region Update

        /// <summary>
        /// Updates a item of type <typeparamref name="TIn"/> into the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <param name="entity">The item to update.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <returns>The <see cref="AzCosmosResponse{TIn}>"/> indicating the result of the operation.</returns>
        public virtual AzCosmosResponse<TIn> UpdateEntity<TIn>(TIn entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null) where TIn : BaseCosmosEntity
        {
            return UpdateEntity<TIn, AzCosmosResponse<TIn>>(entity,
                cancellationToken, databaseId, containerId);
        }

        /// <summary>
        /// Updates a item of type <typeparamref name="TIn"/> into the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{TIn}" /> or a custom model type that inherits from <see cref="AzCosmosResponse{TIn}" />.</typeparam>
        /// <param name="entity">The item to update.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <returns>The <see cref="TOut>"/> indicating the result of the operation.</returns>
        public virtual TOut UpdateEntity<TIn, TOut>(TIn entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null) where TIn : BaseCosmosEntity where TOut : AzCosmosResponse<TIn>, new()
        {
            ThrowIfInvalidEntity(entity);

            return UpdateEntity<TIn, TOut>(entity, entity.Id, entity.PartitionKey,
                cancellationToken, databaseId, containerId, GetConsolidatedDefaultCosmosPartitionKeyPath(containerId));
        }

        /// <summary>
        /// Updates a item of type <typeparamref name="TIn"/> into the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="TIn">A custom model type.</typeparam>
        /// <param name="entity">The item to update.</param>
        /// <param name="id">The Cosmos item id.</param>
        /// <param name="partitionKey">The partition key value of the item.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>The <see cref="AzCosmosResponse{TIn}>"/> indicating the result of the operation.</returns>
        public virtual AzCosmosResponse<TIn> UpdateEntity<TIn>(TIn entity,
            string id,
            string partitionKey,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return UpdateEntity<TIn, AzCosmosResponse<TIn>>(entity, id, partitionKey,
                cancellationToken, databaseId, containerId, partitionKeyPropName);
        }

        /// <summary>        
        /// Updates a item of type <typeparamref name="TIn"/> into the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="TIn">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{TIn}" /> or a custom model type that inherits from <see cref="AzCosmosResponse{TIn}" />.</typeparam>
        /// <param name="entity">The item to add.</param>
        /// <param name="id">The Cosmos item id.</param>
        /// <param name="partitionKey">The value to use as partition key</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>The <see cref="TOut"/> indicating the result of the operation.</returns>
        public virtual TOut UpdateEntity<TIn, TOut>(TIn entity,
            string id,
            string partitionKey,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TOut : AzCosmosResponse<TIn>, new()
        {
            return UpdateEntityAsync<TIn, TOut>(
                entity,
                id,
                partitionKey,
                cancellationToken,
                databaseId,
                containerId,
                partitionKeyPropName).WaitAndUnwrapException();
        }

        #endregion

        #region Update async

        /// <summary>
        /// Updates a item of type <typeparamref name="TIn"/> as an asynchronous operation in the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <param name="entity">The item to update.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <returns>The <see cref="AzCosmosResponse{TIn}>"/> indicating the result of the operation, that was created 
        /// contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.
        /// </returns>
        public virtual async Task<AzCosmosResponse<TIn>> UpdateEntityAsync<TIn>(TIn entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null) where TIn : BaseCosmosEntity
        {
            return await UpdateEntityAsync<TIn, AzCosmosResponse<TIn>>(entity,
                cancellationToken, databaseId, containerId);
        }

        /// <summary>
        /// Updates a item of type <typeparamref name="TIn"/> as an asynchronous operation in the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{TIn}" /> or a custom model type that inherits from <see cref="AzCosmosResponse{TIn}" />.</typeparam>
        /// <param name="entity">The item to update.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <returns>The <see cref="TOut>"/> indicating the result of the operation, that was created 
        /// contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.
        /// </returns>
        public virtual async Task<TOut> UpdateEntityAsync<TIn, TOut>(TIn entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null) where TIn : BaseCosmosEntity where TOut : AzCosmosResponse<TIn>, new()
        {
            ThrowIfInvalidEntity(entity);

            return await UpdateEntityAsync<TIn, TOut>(entity, entity.Id, entity.PartitionKey,
                cancellationToken, databaseId, containerId, GetConsolidatedDefaultCosmosPartitionKeyPath(containerId));
        }

        /// <summary>
        /// Updates a item of type <typeparamref name="TIn"/> as an asynchronous operation in the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="TIn">A custom model type.</typeparam>
        /// <param name="entity">The item to update.</param>
        /// <param name="id">The Cosmos item id.</param>
        /// <param name="partitionKey">The partition key value of the item.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>The <see cref="AzCosmosResponse{TIn}>"/> indicating the result of the operation, that was created 
        /// contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.
        /// </returns>
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

        /// <summary>        
        /// Updates a item of type <typeparamref name="TIn"/> as an asynchronous operation in the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="TIn">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{TIn}" /> or a custom model type that inherits from <see cref="AzCosmosResponse{TIn}" />.</typeparam>
        /// <param name="entity">The item to add.</param>
        /// <param name="id">The Cosmos item id.</param>
        /// <param name="partitionKey">The value to use as partition key</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>The <see cref="TOut"/> indicating the result of the operation, that was created 
        /// contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.
        /// </returns>
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

        #region Update transactionally

        /// <summary>
        /// Updates entities in one or more batch transactions into the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{TransactionalBatchResponse}"/>
        /// with information about each batch execution.</returns>
        public virtual List<AzCosmosResponse<TransactionalBatchResponse>> UpdateEntitiesTransactionally<TIn>(
            IEnumerable<TIn> entities,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
            where TIn : BaseCosmosEntity
        {
            return UpdateEntitiesTransactionally(entities, entt => entt.PartitionKey, entt => entt.Id,
                cancellationToken, databaseId, containerId, partitionKeyPropName);
        }

        /// <summary>
        /// Updates entities in one or more batch transactions into the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="TIn">A custom model type.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="funcGetPartitionKey">Delegate that returns the entity partition key.</param>
        /// <param name="funcGetId">Delegate that returns the entity id.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{TransactionalBatchResponse}"/>
        /// with information about each batch execution.</returns>
        public virtual List<AzCosmosResponse<TransactionalBatchResponse>> UpdateEntitiesTransactionally<TIn>(
            IEnumerable<TIn> entities,
            Func<TIn, string> funcGetPartitionKey,
            Func<TIn, string> funcGetId,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return UpdateEntitiesTransactionallyAsync(entities, funcGetPartitionKey, funcGetId,
                cancellationToken, databaseId, containerId, partitionKeyPropName).WaitAndUnwrapException();
        }

        #endregion
        
        #region Update transactionally async

        /// <summary>
        /// Updates entities in one or more batch transactions into the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{TransactionalBatchResponse}"/>
        /// with information about each batch execution, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<List<AzCosmosResponse<TransactionalBatchResponse>>> UpdateEntitiesTransactionallyAsync<TIn>(
            IEnumerable<TIn> entities,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
            where TIn : BaseCosmosEntity
        {
            return await UpdateEntitiesTransactionallyAsync(entities, entt => entt.PartitionKey, entt => entt.Id,
                cancellationToken, databaseId, containerId, partitionKeyPropName);
        }

        /// <summary>
        /// Updates entities in one or more batch transactions into the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="TIn">A custom model type.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="funcGetPartitionKey">Delegate that returns the entity partition key.</param>
        /// <param name="funcGetId">Delegate that returns the entity id.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{TransactionalBatchResponse}"/>
        /// with information about each batch execution, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<List<AzCosmosResponse<TransactionalBatchResponse>>> UpdateEntitiesTransactionallyAsync<TIn>(
            IEnumerable<TIn> entities,
            Func<TIn, string> funcGetPartitionKey,
            Func<TIn, string> funcGetId,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(funcGetId, nameof(funcGetId));

            return await ExecuteOperationTransactionallyAsync(entities, 
                (azTableTransactionStore, entts) => azTableTransactionStore.ClearNUpdate(entts),
                funcGetPartitionKey, funcGetId, cancellationToken,
                databaseId, containerId, partitionKeyPropName);
        }

        #endregion

        #region Upsert

        /// <summary>
        /// Upserts a item of type <typeparamref name="TIn"/> into the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <param name="entity">The item to upsert.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <returns>The <see cref="AzCosmosResponse{TIn}>"/> indicating the result of the operation.</returns>
        public virtual AzCosmosResponse<TIn> UpsertEntity<TIn>(TIn entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null) where TIn : BaseCosmosEntity
        {
            return UpsertEntity<TIn, AzCosmosResponse<TIn>>(entity, cancellationToken,
                databaseId, containerId);
        }

        /// <summary>
        /// Upserts a item of type <typeparamref name="TIn"/> into the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{TIn}" /> or a custom model type that inherits from <see cref="AzCosmosResponse{TIn}" />.</typeparam>
        /// <param name="entity">The item to upsert.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <returns>The <see cref="TOut>"/> indicating the result of the operation.</returns>
        public virtual TOut UpsertEntity<TIn, TOut>(TIn entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null) where TIn : BaseCosmosEntity where TOut : AzCosmosResponse<TIn>, new()
        {
            ThrowIfInvalidEntity(entity);

            return UpsertEntity<TIn, TOut>(entity, entity.PartitionKey, cancellationToken,
                databaseId, containerId, GetConsolidatedDefaultCosmosPartitionKeyPath(containerId));
        }

        /// <summary>
        /// Upserts a item of type <typeparamref name="TIn"/> into the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="TIn">A custom model type.</typeparam>
        /// <param name="entity">The item to upsert.</param>
        /// <param name="partitionKey">The partition key value of the item.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>The <see cref="AzCosmosResponse{TIn}>"/> indicating the result of the operation.</returns>
        public virtual AzCosmosResponse<TIn> UpsertEntity<TIn>(TIn entity,
            string partitionKey,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return UpsertEntity<TIn, AzCosmosResponse<TIn>>(entity, partitionKey, cancellationToken,
                databaseId, containerId, partitionKeyPropName);
        }

        /// <summary>
        /// Upserts a item of type <typeparamref name="TIn"/> into the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="TIn">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{TIn}" /> or a custom model type that inherits from <see cref="AzCosmosResponse{TIn}" />.</typeparam>
        /// <param name="entity">The item to upsert.</param>
        /// <param name="partitionKey">The value to use as partition key</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>The <see cref="TOut"/> indicating the result of the operation.</returns>
        public virtual TOut UpsertEntity<TIn, TOut>(TIn entity,
            string partitionKey,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TOut : AzCosmosResponse<TIn>, new()
        {
            return UpsertEntityAsync<TIn, TOut>(
                entity,
                partitionKey,
                cancellationToken,
                databaseId,
                containerId,
                partitionKeyPropName).WaitAndUnwrapException();
        }

        #endregion

        #region Upsert async

        /// <summary>
        /// Upserts a item of type <typeparamref name="TIn"/> as an asynchronous operation in the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <param name="entity">The item to upsert.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <returns>The <see cref="AzCosmosResponse{TIn}>"/> indicating the result of the operation, that was created 
        /// contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.
        /// </returns>
        public virtual async Task<AzCosmosResponse<TIn>> UpsertEntityAsync<TIn>(TIn entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null) where TIn : BaseCosmosEntity
        {
            return await UpsertEntityAsync<TIn, AzCosmosResponse<TIn>>(entity, cancellationToken,
                databaseId, containerId);
        }

        /// <summary>
        /// Upserts a item of type <typeparamref name="TIn"/> as an asynchronous operation in the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{TIn}" /> or a custom model type that inherits from <see cref="AzCosmosResponse{TIn}" />.</typeparam>
        /// <param name="entity">The item to upsert.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <returns>The <see cref="TOut>"/> indicating the result of the operation, that was created 
        /// contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.
        /// </returns>
        public virtual async Task<TOut> UpsertEntityAsync<TIn, TOut>(TIn entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null) where TIn : BaseCosmosEntity where TOut : AzCosmosResponse<TIn>, new()
        {
            ThrowIfInvalidEntity(entity);

            return await UpsertEntityAsync<TIn, TOut>(entity, entity.PartitionKey, cancellationToken,
                databaseId, containerId, GetConsolidatedDefaultCosmosPartitionKeyPath(containerId));
        }

        /// <summary>
        /// Upserts a item of type <typeparamref name="TIn"/> as an asynchronous operation in the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="TIn">A custom model type.</typeparam>
        /// <param name="entity">The item to upsert.</param>
        /// <param name="partitionKey">The partition key value of the item.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>The <see cref="AzCosmosResponse{TIn}>"/> indicating the result of the operation, that was created 
        /// contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.
        /// </returns>
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

        /// <summary>
        /// Upserts a item of type <typeparamref name="TIn"/> as an asynchronous operation in the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="TIn">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{TIn}" /> or a custom model type that inherits from <see cref="AzCosmosResponse{TIn}" />.</typeparam>
        /// <param name="entity">The item to upsert.</param>
        /// <param name="partitionKey">The value to use as partition key</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>The <see cref="TOut"/> indicating the result of the operation, that was created 
        /// contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.
        /// </returns>
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

        #region Upsert transactionally

        /// <summary>
        /// Upserts entities in one or more batch transactions into the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{TransactionalBatchResponse}"/>
        /// with information about each batch execution.</returns>
        public virtual List<AzCosmosResponse<TransactionalBatchResponse>> UpsertEntitiesTransactionally<TIn>(
            IEnumerable<TIn> entities,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
            where TIn : BaseCosmosEntity
        {
            return UpsertEntitiesTransactionally(entities, entt => entt.PartitionKey, entt => entt.Id,
                cancellationToken, databaseId, containerId, partitionKeyPropName);
        }

        /// <summary>
        /// Upserts entities in one or more batch transactions into the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="TIn">A custom model type.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="funcGetPartitionKey">Delegate that returns the entity partition key.</param>
        /// <param name="funcGetId">Delegate that returns the entity id.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{TransactionalBatchResponse}"/>
        /// with information about each batch execution.</returns>
        public virtual List<AzCosmosResponse<TransactionalBatchResponse>> UpsertEntitiesTransactionally<TIn>(
            IEnumerable<TIn> entities,
            Func<TIn, string> funcGetPartitionKey,
            Func<TIn, string> funcGetId,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return UpsertEntitiesTransactionallyAsync(entities, funcGetPartitionKey, funcGetId,
                cancellationToken, databaseId, containerId, partitionKeyPropName).WaitAndUnwrapException();
        }

        #endregion

        #region Upsert transactionally async

        /// <summary>
        /// Upserts entities in one or more batch transactions into the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{TransactionalBatchResponse}"/>
        /// with information about each batch execution, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<List<AzCosmosResponse<TransactionalBatchResponse>>> UpsertEntitiesTransactionallyAsync<TIn>(
            IEnumerable<TIn> entities,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
            where TIn : BaseCosmosEntity
        {
            return await UpsertEntitiesTransactionallyAsync(entities, entt => entt.PartitionKey, entt => entt.Id,
                cancellationToken, databaseId, containerId, partitionKeyPropName);
        }

        /// <summary>
        /// Upserts entities in one or more batch transactions into the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="TIn">A custom model type.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="funcGetPartitionKey">Delegate that returns the entity partition key.</param>
        /// <param name="funcGetId">Delegate that returns the entity id.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{TransactionalBatchResponse}"/>
        /// with information about each batch execution, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<List<AzCosmosResponse<TransactionalBatchResponse>>> UpsertEntitiesTransactionallyAsync<TIn>(
            IEnumerable<TIn> entities,
            Func<TIn, string> funcGetPartitionKey,
            Func<TIn, string> funcGetId,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(funcGetId, nameof(funcGetId));

            return await ExecuteOperationTransactionallyAsync(entities,
                (azTableTransactionStore, entts) => azTableTransactionStore.ClearNUpsert(entts),
                funcGetPartitionKey, funcGetId, cancellationToken,
                databaseId, containerId, partitionKeyPropName);
        }

        #endregion

        #region Delete

        /// <summary>
        /// Deletes a item of type <typeparamref name="T"/> from the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <param name="entity">The item to delete.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <returns>The <see cref="AzCosmosResponse{T}>"/> indicating the result of the operation.</returns>
        public virtual AzCosmosResponse<T> DeleteEntity<T>(T entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null) where T : BaseCosmosEntity
        {
            return DeleteEntity<T, AzCosmosResponse<T>>(entity, cancellationToken,
                databaseId, containerId);
        }

        /// <summary>
        /// Deletes a item of type <typeparamref name="T"/> from the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{T}" /> or a custom model type that inherits from <see cref="AzCosmosResponse{T}" />.</typeparam>
        /// <param name="entity">The item to delete.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <returns>The <see cref="TOut>"/> indicating the result of the operation.</returns>
        public virtual TOut DeleteEntity<T, TOut>(T entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null) where T : BaseCosmosEntity where TOut : AzCosmosResponse<T>, new()
        {
            ThrowIfInvalidEntity(entity);

            return DeleteEntity<T, TOut>(entity.Id, entity.PartitionKey, cancellationToken,
                databaseId, containerId, GetConsolidatedDefaultCosmosPartitionKeyPath(containerId));
        }

        /// <summary>
        /// Deletes a item of type <typeparamref name="T"/> from the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="id">The Cosmos item id.</param>
        /// <param name="partitionKey">The partition key value of the item.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>The <see cref="AzCosmosResponse{T}>"/> indicating the result of the operation.</returns>
        public virtual AzCosmosResponse<T> DeleteEntity<T>(string id,
            string partitionKey,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return DeleteEntity<T, AzCosmosResponse<T>>(id, partitionKey, cancellationToken,
                databaseId, containerId, partitionKeyPropName);
        }

        /// <summary>
        /// Deletes a item of type <typeparamref name="T"/> from the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{T}" /> or a custom model type that inherits from <see cref="AzCosmosResponse{T}" />.</typeparam>
        /// <param name="id">The Cosmos item id.</param>
        /// <param name="partitionKey">The partition key value of the item.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>The <see cref="AzCosmosResponse{T}>"/> indicating the result of the operation.</returns>
        public virtual TOut DeleteEntity<T, TOut>(string id,
            string partitionKey,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null) where TOut : AzCosmosResponse<T>, new()
        {
            return DeleteEntityAsync<T, TOut>(id,
                partitionKey,
                cancellationToken,
                databaseId,
                containerId,
                partitionKeyPropName).WaitAndUnwrapException();
        }

        #endregion

        #region Delete async

        /// <summary>
        /// Deletes a item of type <typeparamref name="T"/> as an asynchronous operation in the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <param name="entity">The item to delete.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <returns>The <see cref="AzCosmosResponse{T}>"/> indicating the result of the operation, that was created 
        /// contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.
        /// </returns>
        public virtual async Task<AzCosmosResponse<T>> DeleteEntityAsync<T>(T entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null) where T : BaseCosmosEntity
        {
            return await DeleteEntityAsync<T, AzCosmosResponse<T>>(entity, cancellationToken,
                databaseId, containerId);
        }

        /// <summary>
        /// Deletes a item of type <typeparamref name="T"/> as an asynchronous operation in the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{T}" /> or a custom model type that inherits from <see cref="AzCosmosResponse{T}" />.</typeparam>
        /// <param name="entity">The item to delete.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <returns>The <see cref="TOut>"/> indicating the result of the operation, that was created 
        /// contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.
        /// </returns>
        public virtual async Task<TOut> DeleteEntityAsync<T, TOut>(T entity,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null) where T : BaseCosmosEntity where TOut : AzCosmosResponse<T>, new()
        {
            ThrowIfInvalidEntity(entity);

            return await DeleteEntityAsync<T, TOut>(entity.Id, entity.PartitionKey, cancellationToken,
                databaseId, containerId, GetConsolidatedDefaultCosmosPartitionKeyPath(containerId));
        }

        /// <summary>
        /// Deletes a item of type <typeparamref name="T"/> as an asynchronous operation in the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="id">The Cosmos item id.</param>
        /// <param name="partitionKey">The partition key value of the item.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>The <see cref="AzCosmosResponse{T}>"/> indicating the result of the operation, that was created 
        /// contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.
        /// </returns>
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

        /// <summary>
        /// Deletes a item of type <typeparamref name="T"/> as an asynchronous operation in the Azure Cosmos service.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <typeparam name="TOut">A model of type <see cref="AzCosmosResponse{T}" /> or a custom model type that inherits from <see cref="AzCosmosResponse{T}" />.</typeparam>
        /// <param name="id">The Cosmos item id.</param>
        /// <param name="partitionKey">The partition key value of the item.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database.</param>
        /// <param name="containerId">The Id of the Cosmos container.</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>The <see cref="AzCosmosResponse{T}>"/> indicating the result of the operation, that was created 
        /// contained within a System.Threading.Tasks.Task object representing the service response 
        /// for the asynchronous operation.
        /// </returns>
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

        #region Delete transactionally

        /// <summary>
        /// Deletes entities in one or more batch transactions into the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="T">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{TransactionalBatchResponse}"/>
        /// with information about each batch execution.</returns>
        public virtual List<AzCosmosResponse<TransactionalBatchResponse>> DeleteEntitiesTransactionally<T>(
            IEnumerable<T> entities,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
            where T : BaseCosmosEntity
        {
            return DeleteEntitiesTransactionally(entities, entt => entt.PartitionKey, entt => entt.Id,
                cancellationToken, databaseId, containerId, partitionKeyPropName);
        }

        /// <summary>
        /// Deletes entities in one or more batch transactions into the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="funcGetPartitionKey">Delegate that returns the entity partition key.</param>
        /// <param name="funcGetId">Delegate that returns the entity id.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{TransactionalBatchResponse}"/>
        /// with information about each batch execution.</returns>
        public virtual List<AzCosmosResponse<TransactionalBatchResponse>> DeleteEntitiesTransactionally<T>(
            IEnumerable<T> entities,
            Func<T, string> funcGetPartitionKey,
            Func<T, string> funcGetId,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            return DeleteEntitiesTransactionallyAsync(entities, funcGetPartitionKey, funcGetId,
                cancellationToken, databaseId, containerId, partitionKeyPropName).WaitAndUnwrapException();
        }

        #endregion

        #region Delete transactionally async

        /// <summary>
        /// Deletes entities in one or more batch transactions into the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="T">A custom model type that inherits from <see cref="BaseCosmosEntity" />.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{TransactionalBatchResponse}"/>
        /// with information about each batch execution, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<List<AzCosmosResponse<TransactionalBatchResponse>>> DeleteEntitiesTransactionallyAsync<T>(
            IEnumerable<T> entities,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
            where T : BaseCosmosEntity
        {
            return await DeleteEntitiesTransactionallyAsync(entities, entt => entt.PartitionKey, entt => entt.Id,
                cancellationToken, databaseId, containerId, partitionKeyPropName);
        }

        /// <summary>
        /// Deletes entities in one or more batch transactions into the Azure Cosmos service.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="T">A custom model type.</typeparam>
        /// <param name="entities">The items to add.</param>
        /// <param name="funcGetPartitionKey">Delegate that returns the entity partition key.</param>
        /// <param name="funcGetId">Delegate that returns the entity id.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="databaseId">The Id of the Cosmos database</param>
        /// <param name="containerId">The Id of the Cosmos container</param>
        /// <param name="partitionKeyPropName">The path to the partition key. Example: /PartitionKey</param>
        /// <returns>A collection containing responses of type <see cref="AzCosmosResponse{TransactionalBatchResponse}"/>
        /// with information about each batch execution, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<List<AzCosmosResponse<TransactionalBatchResponse>>> DeleteEntitiesTransactionallyAsync<T>(
            IEnumerable<T> entities,
            Func<T, string> funcGetPartitionKey,
            Func<T, string> funcGetId,
            CancellationToken cancellationToken = default,
            string databaseId = null,
            string containerId = null,
            string partitionKeyPropName = null)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(funcGetId, nameof(funcGetId));

            return await ExecuteOperationTransactionallyAsync(entities,
                (azTableTransactionStore, entts) => azTableTransactionStore.ClearNDelete(entts),
                funcGetPartitionKey, funcGetId, cancellationToken,
                databaseId, containerId, partitionKeyPropName);
        }

        #endregion
    }
}
