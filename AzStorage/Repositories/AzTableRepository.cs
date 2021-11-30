using System;
using System.Collections.Generic;
using System.Threading;
using AzStorage.Core.Helpers;
using AzCoreTools.Core;
using AzCoreTools.Core.Validators;
using AzStorage.Repositories.Core;
using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using CoreTools.Helpers;
using ExThrower = CoreTools.Throws.ExceptionThrower;
using AzCoreTools.Extensions;
using AzStorage.Core.Texting;
using AzStorage.Core.Tables;
using AzStorage.Core.Extensions;
using AzCoreTools.Helpers;
using AzCoreTools.Utilities;
using System.Threading.Tasks;

namespace AzStorage.Repositories
{
    public class AzTableRepository : AzStorageRepository<AzTableRetryOptions>
    {
        protected AzTableRepository() { }

        public AzTableRepository(string connectionString,
            CreateResourcePolicy createResourcePolicy = CreateResourcePolicy.OnlyFirstTime,
            AzTableClientOptions tableClientOptions = null, 
            AzTableRetryOptions retryOptions = null) : base(createResourcePolicy, retryOptions)
        {
            ThrowIfInvalidConnectionString(connectionString);

            ConnectionString = connectionString;
            AzTableClientOptions = tableClientOptions;
        }

        #region Properties

        protected TableServiceClient _TableServiceClient;
        protected virtual TableServiceClient TableServiceClient
        {
            get
            {
                if (_TableServiceClient == null)
                    _TableServiceClient = CreateTableServiceClient();

                return _TableServiceClient;
            }
            private set
            {
                ExThrower.ST_ThrowIfArgumentIsNull(value, nameof(Azure.Data.Tables.TableServiceClient));

                _TableServiceClient = value;
            }
        }

        protected virtual AzTableClientOptions AzTableClientOptions { get; set; }

        #endregion

        #region Protected methods

        protected virtual TableServiceClient CreateTableServiceClient()
        {
            ThrowIfInvalidConnectionString();

            return new TableServiceClient(ConnectionString, CreateClientOptions(AzTableClientOptions));
        }

        protected TableClient _TableClient;
        protected virtual TableClient GetTableClient<T>(string tableName = default) where T : class, ITableEntity, new()
        {
            return GetTableClient(GetTableName<T>(tableName));
        }

        protected virtual TableClient GetTableClient(string tableName)
        {
            var response = CreateOrLoadTableClient(tableName);
            if (response != null && !ResponseValidator.ResponseSucceeded<Response>(response.GetRawResponse()))
                ExThrower.ST_ThrowInvalidOperationException(ErrorTextProvider.Can_not_load_create_table);

            return _TableClient;
        }

        protected virtual string GetTableName<T>(string tableName = default) where T : class
        {
            if (string.IsNullOrEmpty(tableName))
                tableName = typeof(T).Name;

            return tableName;
        }

        protected AzTableTransactionStore CreateAzTableTransactionStore()
        {
            return new AzTableTransactionStore();
        }

        protected List<AzStorageResponse<IReadOnlyList<Response>>> DeleteEntities<T>(AzStorageResponse<List<T>> response,
            CancellationToken cancellationToken = default,
            string tableName = default) where T : class, ITableEntity, new()
        {
            if (!response.Succeeded)
                ExThrower.ST_ThrowInvalidOperationException($"{nameof(response)} opperations must be successful");

            if (response.Value == null)
                ExThrower.ST_ThrowInvalidOperationException(ErrorTextProvider.Error_retrieving_entities);

            return DeleteEntitiesTransactionally(response.Value, cancellationToken, tableName);
        }

        #endregion

        #region TableClient creator

        protected virtual Response<TableItem> CreateOrLoadTableClient<T>(string tableName = default) where T : class
        {
            return CreateOrLoadTableClient(GetTableName<T>(tableName));
        }

        protected virtual Response<TableItem> CreateOrLoadTableClient(string tableName)
        {
            ThrowIfInvalidTableName(tableName);

            return CreateOrLoadTableClient(tableName, CreateTableIfNotExists);
        }

        protected virtual Response<TableItem> CreateOrLoadTableClient(string tableName, Func<dynamic[], Response<TableItem>> func)
        {
            if (_TableClient == null || !tableName.Equals(_TableClient.Name))
                SetTrueToIsFirstTime();

            bool _isFirstTime = IsFirstTimeResourceCreation;

            Response<TableItem> response;
            var result = TryCreateResource(func, new dynamic[] { tableName, default(CancellationToken) }, ref _isFirstTime, out response);

            IsFirstTimeResourceCreation = _isFirstTime;

            if (result && ResponseValidator.CreateResourceResponseSucceeded<Response<TableItem>, TableItem>(response))
                _TableClient = CreateTableClient(tableName);

            return response;
        }

        private Response<TableItem> CreateTableIfNotExists(dynamic[] @params)
        {
            return CreateTableIfNotExists(@params[0], @params[1]);
        }

        private Response<TableItem> CreateTableIfNotExists(string tableName, CancellationToken cancellationToken)
        {
            return TableServiceClient.CreateTableIfNotExists(tableName, cancellationToken);
        }

        protected virtual TableClient CreateTableClient(string tableName)
        {
            ThrowIfInvalidConnectionString();

            return new TableClient(ConnectionString, tableName, CreateClientOptions(AzTableClientOptions));
        }

        #endregion

        #region Throws

        protected virtual void ThrowIfInvalidTableName(string tableName)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrWhitespace(tableName, nameof(tableName), nameof(tableName));
        }

        #endregion

        #region Add

        /// <summary>
        /// Adds a Table Entity of type <typeparamref name="TIn"/> into the Table.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entity">The entity to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse AddEntity<TIn>(TIn entity,
            CancellationToken cancellationToken = default,
            string tableName = default) where TIn : class, ITableEntity, new()
        {
            return AddEntity<TIn, AzStorageResponse>(entity,
                cancellationToken,
                tableName);
        }

        /// <summary>
        /// Adds a Table Entity of type <typeparamref name="TIn"/> into the Table.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <typeparam name="TOut">A custom model type that inherits of <see cref="AzStorageResponse" />
        /// or an instance of <see cref="AzStorageResponse"/>.</typeparam>
        /// <param name="entity">The entity to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>The <typeparamref name="TOut"/> indicating the result of the operation.</returns>
        public virtual TOut AddEntity<TIn, TOut>(TIn entity,
            CancellationToken cancellationToken = default,
            string tableName = default) where TIn : class, ITableEntity, new() where TOut : AzStorageResponse, new()
        {
            return FuncHelper.Execute<TIn, CancellationToken, Response, TOut>(
                GetTableClient<TIn>(tableName).AddEntity, entity, cancellationToken);
        }

        /// <summary>
        /// Adds entities in one or more batch transactions to the service for execution.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entities">The entities to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse"/> with information about each batch execution.</returns>
        public virtual List<AzStorageResponse<IReadOnlyList<Response>>> AddEntitiesTransactionally<TIn>(
            IEnumerable<TIn> entities,
            CancellationToken cancellationToken = default,
            string tableName = default) where TIn : class, ITableEntity, new()
        {
            ExThrower.ST_ThrowIfArgumentIsNull(entities, nameof(entities));

            var azTableTransactionStore = CreateAzTableTransactionStore();
            azTableTransactionStore.ClearNAdd(entities);

            return GetTableClient<TIn>(tableName).SubmitTransaction<AzTableTransactionStore>(
                azTableTransactionStore, cancellationToken);
        }

        #endregion

        #region Add async

        /// <summary>
        /// Adds a Table Entity of type <typeparamref name="TIn"/> into the Table.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entity">The entity to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse> AddEntityAsync<TIn>(TIn entity,
            CancellationToken cancellationToken = default,
            string tableName = default) where TIn : class, ITableEntity, new()
        {
            return await AddEntityAsync<TIn, AzStorageResponse>(entity,
                cancellationToken,
                tableName);
        }

        /// <summary>
        /// Adds a Table Entity of type <typeparamref name="TIn"/> into the Table.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <typeparam name="TOut">A custom model type that inherits of <see cref="AzStorageResponse" /> 
        /// or an instance of <see cref="AzStorageResponse"/>.</typeparam>
        /// <param name="entity">The entity to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>The <typeparamref name="TOut"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<TOut> AddEntityAsync<TIn, TOut>(TIn entity,
            CancellationToken cancellationToken = default,
            string tableName = default) where TIn : class, ITableEntity, new() where TOut : AzStorageResponse, new()
        {
            return await FuncHelper.ExecuteAsync<TIn, CancellationToken, Response, TOut>(
                GetTableClient<TIn>(tableName).AddEntityAsync,
                entity, cancellationToken);
        }

        /// <summary>
        /// Adds a Table Entities of type <typeparamref name="TIn"/> into the Table.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entities">The entities to add.</param>
        /// <param name="addEntityCancellationToken">A <see cref="CancellationToken"/> controlling the add operation lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <param name="parallelOptions">Options that configure the operation of methods on the System.Threading.Tasks.Parallel</param>
        /// <returns>Number of entities added</returns>
        public virtual int AddEntitiesParallelForEach<TIn>(IEnumerable<TIn> entities,
            CancellationToken addEntityCancellationToken = default,
            string tableName = null,
            ParallelOptions parallelOptions = null) where TIn : class, ITableEntity, new()
        {
            return Helper.ExecuteParallelForEach(AddEntity<TIn, AzStorageResponse>, entities, tableName,
                ResponseValidator.ResponseSucceeded,
                addEntityCancellationToken,
                parallelOptions);
        }

        /// <summary>
        /// Adds entities in one or more batch transactions to the service for execution.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entities">The entities to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{IReadOnlyList{Response}}"/>
        /// with information about each batch execution, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<List<AzStorageResponse<IReadOnlyList<Response>>>> AddEntitiesTransactionallyAsync<TIn>(
            IEnumerable<TIn> entities,
            CancellationToken cancellationToken = default,
            string tableName = default) where TIn : class, ITableEntity, new()
        {
            ExThrower.ST_ThrowIfArgumentIsNull(entities, nameof(entities));

            var _azTableTransactionStore = new AzTableTransactionStore();
            _azTableTransactionStore.Add(entities);

            return await GetTableClient<TIn>(tableName).SubmitTransactionAsync<AzTableTransactionStore>(
                _azTableTransactionStore, cancellationToken);
        }

        #endregion

        #region Get

        /// <summary>
        /// Gets the specified table entity of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the table entity.</param>
        /// <param name="rowKey">The row key that identifies the table entity.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse{T}"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse<T> GetEntity<T>(string partitionKey, string rowKey,
            CancellationToken cancellationToken = default,
            string tableName = default) where T : class, ITableEntity, new()
        {
            return FuncHelper.Execute<string, string, IEnumerable<string>, CancellationToken, Response<T>, AzStorageResponse<T>, T>(
                GetTableClient<T>(tableName).GetEntity<T>,
                partitionKey, rowKey, default, cancellationToken);
        }

        #endregion

        #region Get async

        /// <summary>
        /// Gets the specified table entity of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the table entity.</param>
        /// <param name="rowKey">The row key that identifies the table entity.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse{T}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<T>> GetEntityAsync<T>(string partitionKey, string rowKey,
            CancellationToken cancellationToken = default,
            string tableName = default) where T : class, ITableEntity, new()
        {
            return await FuncHelper.ExecuteAsync<string, string, IEnumerable<string>, CancellationToken, Response<T>, AzStorageResponse<T>, T>(
                GetTableClient<T>(tableName).GetEntityAsync<T>,
                partitionKey, rowKey, default, cancellationToken);
        }

        #endregion

        #region Query

        /// <summary>
        /// Queries all entities in the table.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse{List{T}}"/> containing a collection of entity models serialized as type <typeparamref name="T"/>.</returns>
        public virtual AzStorageResponse<List<T>> QueryAll<T>(
            CancellationToken cancellationToken = default,
            int take = int.MaxValue,
            string tableName = default) where T : class, ITableEntity, new()
        {
            return FuncHelper.Execute<CancellationToken, int, AzStorageResponse<List<T>>, AzStorageResponse<List<T>>, List<T>>(
                GetTableClient<T>(tableName).QueryAll<T>,
                cancellationToken, take);
        }

        /// <summary>
        /// Queries entities in the table with the specified partition key.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse{List{T}}"/> containing a collection of entity models serialized as type <typeparamref name="T"/>.</returns>
        public virtual AzStorageResponse<List<T>> QueryByPartitionKey<T>(
            string partitionKey,
            CancellationToken cancellationToken = default,
            int take = int.MaxValue,
            string tableName = default) where T : class, ITableEntity, new()
        {
            return FuncHelper.Execute<string, CancellationToken, int, AzStorageResponse<List<T>>, AzStorageResponse<List<T>>, List<T>>(
                GetTableClient<T>(tableName).QueryByPartitionKey<T>,
                partitionKey, cancellationToken, take);
        }

        /// <summary>
        /// Queries entities in the table with the specified partition key start pattern.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="startPattern">The partition key start pattern that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse{List{T}}"/> containing a collection of entity models serialized as type <typeparamref name="T"/>.</returns>
        public virtual AzStorageResponse<List<T>> QueryByPartitionKeyStartPattern<T>(
            string startPattern,
            CancellationToken cancellationToken = default,
            int take = int.MaxValue,
            string tableName = default) where T : class, ITableEntity, new()
        {
            return FuncHelper.Execute<string, CancellationToken, int, AzStorageResponse<List<T>>, AzStorageResponse<List<T>>, List<T>>(
                GetTableClient<T>(tableName).QueryByPartitionKeyStartPattern<T>,
                startPattern, cancellationToken, take);
        }

        /// <summary>
        /// Gets the specified table entity of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the table entity.</param>
        /// <param name="rowKey">The row key that identifies the table entity.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse{T}"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse<T> QueryByPartitionKeyRowKey<T>(
            string partitionKey,
            string rowKey,
            CancellationToken cancellationToken = default,
            string tableName = default) where T : class, ITableEntity, new()
        {
            return FuncHelper.Execute<string, string, CancellationToken, AzStorageResponse<T>, AzStorageResponse<T>, T>(
                GetTableClient<T>(tableName).QueryByPartitionKeyRowKey<T>,
                partitionKey, rowKey, cancellationToken);
        }

        /// <summary>
        /// Queries entities in the table with the specified partition key and row key start pattern.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the entities.</param>
        /// <param name="rowKeyStartPattern">The row key start pattern that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse{List{T}}"/> containing a collection of entity models serialized as type <typeparamref name="T"/>.</returns>
        public virtual AzStorageResponse<List<T>> QueryByPartitionKeyRowKeyStartPattern<T>(
            string partitionKey,
            string rowKeyStartPattern,
            CancellationToken cancellationToken = default,
            int take = int.MaxValue,
            string tableName = default) where T : class, ITableEntity, new()
        {
            return FuncHelper.Execute<string, string, CancellationToken, int, AzStorageResponse<List<T>>, AzStorageResponse<List<T>>, List<T>>(
                GetTableClient<T>(tableName).QueryByPartitionKeyRowKeyStartPattern<T>,
                partitionKey, rowKeyStartPattern, cancellationToken, take);
        }

        /// <summary>
        /// Queries entities in the table with the specified partition key start pattern and row key start pattern.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKeyStartPattern">The partition key start pattern that identifies the entities.</param>
        /// <param name="rowKeyStartPattern">The row key start pattern that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse{List{T}}"/> containing a collection of entity models serialized as type <typeparamref name="T"/>.</returns>
        public virtual AzStorageResponse<List<T>> QueryByPartitionKeyStartPatternRowKeyStartPattern<T>(
            string partitionKeyStartPattern,
            string rowKeyStartPattern,
            CancellationToken cancellationToken = default,
            int take = int.MaxValue,
            string tableName = default) where T : class, ITableEntity, new()
        {
            return FuncHelper.Execute<string, string, CancellationToken, int, AzStorageResponse<List<T>>, AzStorageResponse<List<T>>, List<T>>(
                GetTableClient<T>(tableName).QueryByPartitionKeyStartPatternRowKeyStartPattern<T>,
                partitionKeyStartPattern, rowKeyStartPattern, cancellationToken, take);
        }

        /// <summary>
        /// Queries entities in the table between the specified start timestamp and end timestamp.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="timeStampFrom">The start timestamp that identifies the entities.</param>
        /// <param name="timeStampTo">The end timestamp that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse{List{T}}"/> containing a collection of entity models serialized as type <typeparamref name="T"/>.</returns>
        public virtual AzStorageResponse<List<T>> QueryByTimestamp<T>(
            DateTime timeStampFrom,
            DateTime timeStampTo,
            CancellationToken cancellationToken = default,
            int take = int.MaxValue,
            string tableName = default) where T : class, ITableEntity, new()
        {
            return FuncHelper.Execute<DateTime, DateTime, CancellationToken, int, AzStorageResponse<List<T>>, AzStorageResponse<List<T>>, List<T>>(
                GetTableClient<T>(tableName).QueryByTimestamp<T>,
                timeStampFrom, timeStampTo, cancellationToken, take);
        }

        /// <summary>
        /// Queries entities in the table with the specified partition key and between start timestamp and end timestamp.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the entities.</param>
        /// <param name="timeStampFrom">The start timestamp that identifies the entities.</param>
        /// <param name="timeStampTo">The end timestamp that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse{List{T}}"/> containing a collection of entity models serialized as type <typeparamref name="T"/>.</returns>
        public virtual AzStorageResponse<List<T>> QueryByPartitionKeyTimestamp<T>(
            string partitionKey,
            DateTime timeStampFrom,
            DateTime timeStampTo,
            CancellationToken cancellationToken = default,
            int take = int.MaxValue,
            string tableName = default) where T : class, ITableEntity, new()
        {
            return FuncHelper.Execute<string, DateTime, DateTime, CancellationToken, int, AzStorageResponse<List<T>>, AzStorageResponse<List<T>>, List<T>>(
                GetTableClient<T>(tableName).QueryByPartitionKeyTimestamp<T>,
                partitionKey, timeStampFrom, timeStampTo, cancellationToken, take);
        }

        #endregion

        #region Query async

        /// <summary>
        /// Queries all entities in the table.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse{List{T}}"/> containing a collection of entity models serialized as type <typeparamref name="T"/>, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<List<T>>> QueryAllAsync<T>(
            CancellationToken cancellationToken = default,
            int take = int.MaxValue,
            string tableName = default) where T : class, ITableEntity, new()
        {
            return await FuncHelper.ExecuteAsync<CancellationToken, int, AzStorageResponse<List<T>>, AzStorageResponse<List<T>>, List<T>>(
                GetTableClient<T>(tableName).QueryAllAsync<T>,
                cancellationToken, take);
        }

        /// <summary>
        /// Queries entities in the table with the specified partition key.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse{List{T}}"/> containing a collection of entity models serialized as type <typeparamref name="T"/>, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<List<T>>> QueryByPartitionKeyAsync<T>(
            string partitionKey,
            CancellationToken cancellationToken = default,
            int take = int.MaxValue,
            string tableName = default) where T : class, ITableEntity, new()
        {
            return await FuncHelper.ExecuteAsync<string, CancellationToken, int, AzStorageResponse<List<T>>, AzStorageResponse<List<T>>, List<T>>(
                GetTableClient<T>(tableName).QueryByPartitionKeyAsync<T>,
                partitionKey, cancellationToken, take);
        }

        /// <summary>
        /// Queries entities in the table with the specified partition key start pattern.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="startPattern">The partition key start pattern that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse{List{T}}"/> containing a collection of entity models serialized as type <typeparamref name="T"/>, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<List<T>>> QueryByPartitionKeyStartPatternAsync<T>(
            string startPattern,
            CancellationToken cancellationToken = default,
            int take = int.MaxValue,
            string tableName = default) where T : class, ITableEntity, new()
        {
            return await FuncHelper.ExecuteAsync<string, CancellationToken, int, AzStorageResponse<List<T>>, AzStorageResponse<List<T>>, List<T>>(
                GetTableClient<T>(tableName).QueryByPartitionKeyStartPatternAsync<T>,
                startPattern, cancellationToken, take);
        }

        /// <summary>
        /// Gets the specified table entity of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the table entity.</param>
        /// <param name="rowKey">The row key that identifies the table entity.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse{T}"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<T>> QueryByPartitionKeyRowKeyAsync<T>(
            string partitionKey,
            string rowKey,
            CancellationToken cancellationToken = default,
            string tableName = default) where T : class, ITableEntity, new()
        {
            return await FuncHelper.ExecuteAsync<string, string, CancellationToken, AzStorageResponse<T>, AzStorageResponse<T>, T>(
                GetTableClient<T>(tableName).QueryByPartitionKeyRowKeyAsync<T>,
                partitionKey, rowKey, cancellationToken);
        }

        /// <summary>
        /// Queries entities in the table with the specified partition key and row key start pattern.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the entities.</param>
        /// <param name="rowKeyStartPattern">The row key start pattern that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse{List{T}}"/> containing a collection of entity models serialized as type <typeparamref name="T"/>, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<List<T>>> QueryByPartitionKeyRowKeyStartPatternAsync<T>(
            string partitionKey,
            string rowKeyStartPattern,
            CancellationToken cancellationToken = default,
            int take = int.MaxValue,
            string tableName = default) where T : class, ITableEntity, new()
        {
            return await FuncHelper.ExecuteAsync<string, string, CancellationToken, int, AzStorageResponse<List<T>>, AzStorageResponse<List<T>>, List<T>>(
                GetTableClient<T>(tableName).QueryByPartitionKeyRowKeyStartPatternAsync<T>,
                partitionKey, rowKeyStartPattern, cancellationToken, take);
        }

        /// <summary>
        /// Queries entities in the table with the specified partition key start pattern and row key start pattern.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKeyStartPattern">The partition key start pattern that identifies the entities.</param>
        /// <param name="rowKeyStartPattern">The row key start pattern that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse{List{T}}"/> containing a collection of entity models serialized as type <typeparamref name="T"/>, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<List<T>>> QueryByPartitionKeyStartPatternRowKeyStartPatternAsync<T>(
            string partitionKeyStartPattern,
            string rowKeyStartPattern,
            CancellationToken cancellationToken = default,
            int take = int.MaxValue,
            string tableName = default) where T : class, ITableEntity, new()
        {
            return await FuncHelper.ExecuteAsync<string, string, CancellationToken, int, AzStorageResponse<List<T>>, AzStorageResponse<List<T>>, List<T>>(
                GetTableClient<T>(tableName).QueryByPartitionKeyStartPatternRowKeyStartPatternAsync<T>,
                partitionKeyStartPattern, rowKeyStartPattern, cancellationToken, take);
        }

        /// <summary>
        /// Queries entities in the table between the specified start timestamp and end timestamp.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="timeStampFrom">The start timestamp that identifies the entities.</param>
        /// <param name="timeStampTo">The end timestamp that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse{List{T}}"/> containing a collection of entity models serialized as type <typeparamref name="T"/>, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<List<T>>> QueryByTimestampAsync<T>(
            DateTime timeStampFrom,
            DateTime timeStampTo,
            CancellationToken cancellationToken = default,
            int take = int.MaxValue,
            string tableName = default) where T : class, ITableEntity, new()
        {
            return await FuncHelper.ExecuteAsync<DateTime, DateTime, CancellationToken, int, AzStorageResponse<List<T>>, AzStorageResponse<List<T>>, List<T>>(
                GetTableClient<T>(tableName).QueryByTimestampAsync<T>,
                timeStampFrom, timeStampTo, cancellationToken, take);
        }

        /// <summary>
        /// Queries entities in the table with the specified partition key and between start timestamp and end timestamp.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the entities.</param>
        /// <param name="timeStampFrom">The start timestamp that identifies the entities.</param>
        /// <param name="timeStampTo">The end timestamp that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse{List{T}}"/> containing a collection of entity models serialized as type <typeparamref name="T"/>, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse<List<T>>> QueryByPartitionKeyTimestampAsync<T>(
            string partitionKey,
            DateTime timeStampFrom,
            DateTime timeStampTo,
            CancellationToken cancellationToken = default,
            int take = int.MaxValue,
            string tableName = default) where T : class, ITableEntity, new()
        {
            return await FuncHelper.ExecuteAsync<string, DateTime, DateTime, CancellationToken, int, AzStorageResponse<List<T>>, AzStorageResponse<List<T>>, List<T>>(
                GetTableClient<T>(tableName).QueryByPartitionKeyTimestampAsync<T>,
                partitionKey, timeStampFrom, timeStampTo, cancellationToken, take);
        }

        #endregion

        #region Update

        /// <summary>
        /// Updates the specified table entity of type <typeparamref name="TIn"/>, if it exists.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Replace"/>, the entity will be replaced.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Merge"/>, the property values present in the
        /// <paramref name="entity"/> will be merged with the existing entity.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entity">The entity to update.</param>
        /// <param name="mode">Determines the behavior of the Update operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse UpdateEntity<TIn>(TIn entity,
            TableUpdateMode mode = TableUpdateMode.Merge,
            CancellationToken cancellationToken = default,
            string tableName = default) where TIn : class, ITableEntity, new()
        {
            return UpdateEntity<TIn, AzStorageResponse>(entity,
                mode,
                cancellationToken,
                tableName);
        }

        /// <summary>
        /// Updates the specified table entity of type <typeparamref name="TIn"/>, if it exists.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Replace"/>, the entity will be replaced.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Merge"/>, the property values present in the
        /// <paramref name="entity"/> will be merged with the existing entity.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <typeparam name="TOut">A custom model type that inherits of <see cref="AzStorageResponse" /> 
        /// or an instance of <see cref="AzStorageResponse"/>.</typeparam>
        /// <param name="entity">The entity to update.</param>
        /// <param name="mode">Determines the behavior of the Update operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>The <typeparamref name="TOut"/> indicating the result of the operation.</returns>
        public virtual TOut UpdateEntity<TIn, TOut>(TIn entity,
            TableUpdateMode mode = TableUpdateMode.Merge,
            CancellationToken cancellationToken = default,
            string tableName = default) where TIn : class, ITableEntity, new() where TOut : AzStorageResponse, new()
        {
            return FuncHelper.Execute<TIn, ETag, TableUpdateMode, CancellationToken, Response, TOut>(
                GetTableClient<TIn>(tableName).UpdateEntity, entity, AzStorageHelper.GetValidETag(entity), mode,
                cancellationToken);
        }

        /// <summary>
        /// Updates entities in one or more batch transactions to the service for execution, if they exists.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Replace"/>, the entities will be replaced.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Merge"/>, the property values present in the
        /// <paramref name="entities"/> will be merged with the existing entities.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entities">The entities to update.</param>
        /// <param name="mode">Determines the behavior of the Update operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{IReadOnlyList{Response}}"/> 
        /// with information about each batch execution.</returns>
        public virtual List<AzStorageResponse<IReadOnlyList<Response>>> UpdateEntitiesTransactionally<TIn>(
            IEnumerable<TIn> entities,
            TableUpdateMode mode = TableUpdateMode.Merge,
            CancellationToken cancellationToken = default,
            string tableName = default) where TIn : class, ITableEntity, new()
        {
            ExThrower.ST_ThrowIfArgumentIsNull(entities, nameof(entities));

            var azTableTransactionStore = CreateAzTableTransactionStore();
            azTableTransactionStore.ClearNUpdate(entities, mode);

            return GetTableClient<TIn>(tableName).SubmitTransaction<AzTableTransactionStore>(
                azTableTransactionStore, cancellationToken);
        }

        #endregion

        #region Update async

        /// <summary>
        /// Updates the specified table entity of type <typeparamref name="TIn"/>, if it exists.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Replace"/>, the entity will be replaced.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Merge"/>, the property values present in the
        /// <paramref name="entity"/> will be merged with the existing entity.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entity">The entity to update.</param>
        /// <param name="mode">Determines the behavior of the Update operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse> UpdateEntityAsync<TIn>(TIn entity,
            TableUpdateMode mode = TableUpdateMode.Merge,
            CancellationToken cancellationToken = default,
            string tableName = default) where TIn : class, ITableEntity, new()
        {
            return await UpdateEntityAsync<TIn, AzStorageResponse>(entity,
                mode,
                cancellationToken,
                tableName);
        }

        /// <summary>
        /// Updates the specified table entity of type <typeparamref name="TIn"/>, if it exists.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Replace"/>, the entity will be replaced.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Merge"/>, the property values present in the
        /// <paramref name="entity"/> will be merged with the existing entity.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <typeparam name="TOut">A custom model type that inherits of <see cref="AzStorageResponse" /> 
        /// or an instance of <see cref="AzStorageResponse"/>.</typeparam>
        /// <param name="entity">The entity to update.</param>
        /// <param name="mode">Determines the behavior of the Update operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>The <typeparamref name="TOut"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<TOut> UpdateEntityAsync<TIn, TOut>(TIn entity,
            TableUpdateMode mode = TableUpdateMode.Merge,
            CancellationToken cancellationToken = default,
            string tableName = default) where TIn : class, ITableEntity, new() where TOut : AzStorageResponse, new()
        {
            return await FuncHelper.ExecuteAsync<TIn, ETag, TableUpdateMode, CancellationToken, Response, TOut>(
                GetTableClient<TIn>(tableName).UpdateEntityAsync,
                entity, AzStorageHelper.GetValidETag(entity), mode, cancellationToken);
        }

        /// <summary>
        /// Updates entities in one or more batch transactions to the service for execution, if they exists.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Replace"/>, the entities will be replaced.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Merge"/>, the property values present in the
        /// <paramref name="entities"/> will be merged with the existing entities.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entities">The entities to update.</param>
        /// <param name="mode">Determines the behavior of the Update operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{IReadOnlyList{Response}}"/> 
        /// with information about each batch execution, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<List<AzStorageResponse<IReadOnlyList<Response>>>> UpdateEntitiesTransactionallyAsync<TIn>(
            IEnumerable<TIn> entities,
            TableUpdateMode mode = TableUpdateMode.Merge,
            CancellationToken cancellationToken = default,
            string tableName = default) where TIn : class, ITableEntity, new()
        {
            ExThrower.ST_ThrowIfArgumentIsNull(entities, nameof(entities));

            var _azTableTransactionStore = new AzTableTransactionStore();
            _azTableTransactionStore.Update(entities, mode);

            return await GetTableClient<TIn>(tableName).SubmitTransactionAsync<AzTableTransactionStore>(
                _azTableTransactionStore, cancellationToken);
        }

        #endregion

        #region Upsert

        /// <summary>
        /// Replaces the specified table entity of type <typeparamref name="TIn"/>, if it exists. 
        /// Creates the entity if it does not exist.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Replace"/> and the entity exists, the entity will be replaced.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Merge"/> and the entity exists, the property values present in the
        /// <paramref name="entity"/> will be merged with the existing entity.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entity">The entity to upsert.</param>
        /// <param name="mode">Determines the behavior of the update operation when the entity already exists in the table.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse UpsertEntity<TIn>(TIn entity,
            TableUpdateMode mode = TableUpdateMode.Merge,
            CancellationToken cancellationToken = default,
            string tableName = default) where TIn : class, ITableEntity, new()
        {
            return UpsertEntity<TIn, AzStorageResponse>(entity,
               mode,
               cancellationToken,
               tableName);
        }

        /// <summary>
        /// Replaces the specified table entity of type <typeparamref name="TIn"/>, if it exists. 
        /// Creates the entity if it does not exist.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Replace"/> and the entity exists, the entity will be replaced.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Merge"/> and the entity exists, the property values present in the
        /// <paramref name="entity"/> will be merged with the existing entity.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <typeparam name="TOut">A custom model type that inherits of <see cref="AzStorageResponse" /> 
        /// or an instance of <see cref="AzStorageResponse"/>.</typeparam>
        /// <param name="entity">The entity to upsert.</param>
        /// <param name="mode">Determines the behavior of the update operation when the entity already exists in the table.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>The <typeparamref name="TOut"/> indicating the result of the operation.</returns>
        public virtual TOut UpsertEntity<TIn, TOut>(TIn entity,
            TableUpdateMode mode = TableUpdateMode.Merge,
            CancellationToken cancellationToken = default,
            string tableName = default) where TIn : class, ITableEntity, new() where TOut : AzStorageResponse, new()
        {
            return FuncHelper.Execute<TIn, TableUpdateMode, CancellationToken, Response, TOut>(
                GetTableClient<TIn>(tableName).UpsertEntity,
                entity, mode, cancellationToken);
        }

        /// <summary>
        /// Replaces entities in one or more batch transactions to the service for execution, if they exists. 
        /// Creates the entity if it does not exist.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Replace"/> and the entity exists, the entity will be replaced.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Merge"/> and the entity exists, the property values present in the
        /// <paramref name="entities"/> will be merged with the existing entity.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entities">The entities to upsert.</param>
        /// <param name="mode">Determines the behavior of the update operation when the entity already exists in the table.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{IReadOnlyList{Response}}"/> 
        /// with information about each batch execution.</returns>
        public virtual List<AzStorageResponse<IReadOnlyList<Response>>> UpsertEntitiesTransactionally<TIn>(IEnumerable<TIn> entities,
            TableUpdateMode mode = TableUpdateMode.Merge,
            CancellationToken cancellationToken = default,
            string tableName = default) where TIn : class, ITableEntity, new()
        {
            ExThrower.ST_ThrowIfArgumentIsNull(entities, nameof(entities));

            var azTableTransactionStore = CreateAzTableTransactionStore();
            azTableTransactionStore.ClearNUpsert(entities, mode);

            return GetTableClient<TIn>(tableName).SubmitTransaction<AzTableTransactionStore>(
                azTableTransactionStore, cancellationToken);
        }

        #endregion

        #region Upsert async

        /// <summary>
        /// Replaces the specified table entity of type <typeparamref name="TIn"/>, if it exists. Creates the entity if it does not exist.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Replace"/> and the entity exists, the entity will be replaced.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Merge"/> and the entity exists, the property values present in the
        /// <paramref name="entity"/> will be merged with the existing entity.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entity">The entity to upsert.</param>
        /// <param name="mode">Determines the behavior of the update operation when the entity already exists in the table.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse> UpsertEntityAsync<TIn>(TIn entity,
            TableUpdateMode mode = TableUpdateMode.Merge,
            CancellationToken cancellationToken = default,
            string tableName = default) where TIn : class, ITableEntity, new()
        {
            return await UpsertEntityAsync<TIn, AzStorageResponse>(entity,
            mode,
            cancellationToken,
            tableName);
        }

        /// <summary>
        /// Replaces the specified table entity of type <typeparamref name="TIn"/>, if it exists. Creates the entity if it does not exist.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Replace"/> and the entity exists, the entity will be replaced.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Merge"/> and the entity exists, the property values present in the
        /// <paramref name="entity"/> will be merged with the existing entity.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <typeparam name="TOut">A custom model type that inherits of <see cref="AzStorageResponse" /> or an instance of <see cref="AzStorageResponse"/>.</typeparam>
        /// <param name="entity">The entity to upsert.</param>
        /// <param name="mode">Determines the behavior of the update operation when the entity already exists in the table.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>The <typeparamref name="TOut"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<TOut> UpsertEntityAsync<TIn, TOut>(TIn entity,
            TableUpdateMode mode = TableUpdateMode.Merge,
            CancellationToken cancellationToken = default,
            string tableName = default) where TIn : class, ITableEntity, new() where TOut : AzStorageResponse, new()
        {
            return await FuncHelper.ExecuteAsync<TIn, TableUpdateMode, CancellationToken, Response, TOut>(
                GetTableClient<TIn>(tableName).UpsertEntityAsync,
                entity, mode, cancellationToken);
        }

        /// <summary>
        /// Replaces entities in one or more batch transactions to the service for execution, if they exists. 
        /// Creates the entity if it does not exist.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Replace"/> and the entity exists, the entity will be replaced.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Merge"/> and the entity exists, the property values present in the
        /// <paramref name="entities"/> will be merged with the existing entity.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entities">The entities to upsert.</param>
        /// <param name="mode">Determines the behavior of the update operation when the entity already exists in the table.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{IReadOnlyList{Response}}"/> 
        /// with information about each batch execution, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<List<AzStorageResponse<IReadOnlyList<Response>>>> UpsertEntitiesTransactionallyAsync<TIn>(IEnumerable<TIn> entities,
            TableUpdateMode mode = TableUpdateMode.Merge,
            CancellationToken cancellationToken = default,
            string tableName = default) where TIn : class, ITableEntity, new()
        {
            ExThrower.ST_ThrowIfArgumentIsNull(entities, nameof(entities));

            var _azTableTransactionStore = new AzTableTransactionStore();
            _azTableTransactionStore.Upsert(entities, mode);

            return await GetTableClient<TIn>(tableName).SubmitTransactionAsync<AzTableTransactionStore>(
                _azTableTransactionStore, cancellationToken);
        }

        #endregion

        #region Delete

        /// <summary>
        /// Deletes the specified table entity.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entity">The entity to delete.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse DeleteEntity<T>(T entity,
            CancellationToken cancellationToken = default,
            string tableName = default) where T : class, ITableEntity, new()
        {
            return DeleteEntity<T, AzStorageResponse>(entity,
                cancellationToken,
                tableName);
        }

        /// <summary>
        /// Deletes the specified table entity.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <typeparam name="TOut">A custom model type that inherits of <see cref="AzStorageResponse" /> or an instance of <see cref="AzStorageResponse"/>.</typeparam>
        /// <param name="entity">The entity to delete.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>The <typeparamref name="TOut"/> indicating the result of the operation.</returns>
        public virtual TOut DeleteEntity<T, TOut>(T entity,
            CancellationToken cancellationToken = default,
            string tableName = default) where T : class, ITableEntity, new() where TOut : AzStorageResponse, new()
        {
            ExThrower.ST_ThrowIfArgumentIsNull(entity, nameof(entity));

            return DeleteEntity<T, TOut>(entity.PartitionKey, entity.RowKey,
                entity.ETag,
                cancellationToken,
                tableName);
        }

        /// <summary>
        /// Deletes the specified table entity.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the table entity.</param>
        /// <param name="rowKey">The row key that identifies the table entity.</param>
        /// <param name="ifMatch">
        /// The If-Match value to be used for optimistic concurrency.
        /// If <see cref="ETag.All"/> is specified, the operation will be executed unconditionally.
        /// If the <see cref="ITableEntity.ETag"/> value is specified, the operation will fail with a status of 412 (Precondition Failed)
        /// if the <see cref="ETag"/> value of the entity in the table does not match.
        /// The default is to delete unconditionally.
        /// </param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse DeleteEntity<T>(string partitionKey, string rowKey,
            ETag ifMatch = default,
            CancellationToken cancellationToken = default,
            string tableName = default) where T : class, ITableEntity, new()
        {
            return DeleteEntity<T, AzStorageResponse>(partitionKey, rowKey,
                ifMatch,
                cancellationToken,
                tableName);
        }

        /// <summary>
        /// Deletes the specified table entity.
        /// </summary>
        /// <param name="partitionKey">The partition key that identifies the table entity.</param>
        /// <param name="rowKey">The row key that identifies the table entity.</param>
        /// <param name="tableName">The table name to execute the operation.</param>
        /// <param name="ifMatch">
        /// The If-Match value to be used for optimistic concurrency.
        /// If <see cref="ETag.All"/> is specified, the operation will be executed unconditionally.
        /// If the <see cref="ITableEntity.ETag"/> value is specified, the operation will fail with a status of 412 (Precondition Failed)
        /// if the <see cref="ETag"/> value of the entity in the table does not match.
        /// The default is to delete unconditionally.
        /// </param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse DeleteEntity(string partitionKey, string rowKey,
            string tableName,
            ETag ifMatch = default,
            CancellationToken cancellationToken = default)
        {
            return DeleteEntity<AzStorageResponse>(partitionKey, rowKey,
                tableName,
                ifMatch,
                cancellationToken);
        }

        /// <summary>
        /// Deletes the specified table entity.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <typeparam name="TOut">A custom model type that inherits of <see cref="AzStorageResponse" /> 
        /// or an instance of <see cref="AzStorageResponse"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the table entity.</param>
        /// <param name="rowKey">The row key that identifies the table entity.</param>
        /// <param name="ifMatch">
        /// The If-Match value to be used for optimistic concurrency.
        /// If <see cref="ETag.All"/> is specified, the operation will be executed unconditionally.
        /// If the <see cref="ITableEntity.ETag"/> value is specified, the operation will fail with a status of 412 (Precondition Failed)
        /// if the <see cref="ETag"/> value of the entity in the table does not match.
        /// The default is to delete unconditionally.
        /// </param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>The <typeparamref name="TOut"/> indicating the result of the operation.</returns>
        public virtual TOut DeleteEntity<T, TOut>(string partitionKey, string rowKey,
            ETag ifMatch = default,
            CancellationToken cancellationToken = default,
            string tableName = default) where T : class, ITableEntity, new() where TOut : AzStorageResponse, new()
        {
            return FuncHelper.Execute<string, string, ETag, CancellationToken, Response, TOut>(
                GetTableClient<T>(tableName).DeleteEntity,
                partitionKey, rowKey, AzStorageHelper.GetValidETag(ifMatch), cancellationToken);
        }

        /// <summary>
        /// Deletes the specified table entity.
        /// </summary>
        /// <typeparam name="TOut">A custom model type that inherits of <see cref="AzStorageResponse" /> 
        /// or an instance of <see cref="AzStorageResponse"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the table entity.</param>
        /// <param name="rowKey">The row key that identifies the table entity.</param>
        /// <param name="tableName">The table name to execute the operation.</param>
        /// <param name="ifMatch">
        /// The If-Match value to be used for optimistic concurrency.
        /// If <see cref="ETag.All"/> is specified, the operation will be executed unconditionally.
        /// If the <see cref="ITableEntity.ETag"/> value is specified, the operation will fail with a status of 412 (Precondition Failed)
        /// if the <see cref="ETag"/> value of the entity in the table does not match.
        /// The default is to delete unconditionally.
        /// </param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <typeparamref name="TOut"/> indicating the result of the operation.</returns>
        public virtual TOut DeleteEntity<TOut>(string partitionKey, string rowKey,
            string tableName,
            ETag ifMatch = default,
            CancellationToken cancellationToken = default) where TOut : AzStorageResponse, new()
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrWhitespace(tableName, nameof(tableName), nameof(tableName));

            return FuncHelper.Execute<string, string, ETag, CancellationToken, Response, TOut>(
                GetTableClient(tableName).DeleteEntity,
                partitionKey, rowKey, AzStorageHelper.GetValidETag(ifMatch), cancellationToken);
        }

        /// <summary>
        /// Deletes entities in the table with the specified partition key, 
        /// in one or more batch transactions to the service for execution.
        /// The sub-operations contained in one batch will either succeed 
        /// or fail together as a transaction.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <param name="amount">Number of entities to delete</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{IReadOnlyList{Response}}"/> 
        /// with information about each batch execution.</returns>
        public virtual List<AzStorageResponse<IReadOnlyList<Response>>> DeleteEntitiesByPartitionKey<T>(
            string partitionKey,
            CancellationToken cancellationToken = default,
            string tableName = null,
            int amount = ConstProvider.DefaultTake) where T : class, ITableEntity, new()
        {
            var entitiesToDel = QueryByPartitionKey<T>(partitionKey, cancellationToken, amount, tableName);

            return DeleteEntities(entitiesToDel, cancellationToken, tableName);
        }

        /// <summary>
        /// Deletes entities in the table with the specified partition key, 
        /// in one or more batch transactions to the service for execution.
        /// The sub-operations contained in one batch will either succeed 
        /// or fail together as a transaction.
        /// </summary>
        /// <param name="partitionKey">The partition key that identifies the entities.</param>
        /// <param name="tableName">The table name to execute the operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="amount">Number of entities to delete</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{IReadOnlyList{Response}}"/> 
        /// with information about each batch execution.</returns>
        public virtual List<AzStorageResponse<IReadOnlyList<Response>>> DeleteEntitiesByPartitionKey(
            string partitionKey,
            string tableName,
            CancellationToken cancellationToken = default,
            int amount = ConstProvider.DefaultTake)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrWhitespace(tableName, nameof(tableName), nameof(tableName));

            var entitiesToDel = QueryByPartitionKey<TableEntity>(partitionKey, cancellationToken, amount, tableName);

            return DeleteEntities(entitiesToDel, cancellationToken, tableName);
        }

        /// <summary>
        /// Deletes entities in the table with the specified partition key start pattern, 
        /// in one or more batch transactions to the service for execution.
        /// The sub-operations contained in one batch will either succeed 
        /// or fail together as a transaction.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKeyStartPattern">The partition key start pattern that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <param name="amount">Number of entities to delete</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{IReadOnlyList{Response}}"/> 
        /// with information about each batch execution.</returns>
        public virtual List<AzStorageResponse<IReadOnlyList<Response>>> DeleteEntitiesByPartitionKeyStartPattern<T>(
            string partitionKeyStartPattern,
            CancellationToken cancellationToken = default,
            string tableName = null,
            int amount = ConstProvider.DefaultTake) where T : class, ITableEntity, new()
        {
            var entitiesToDel = QueryByPartitionKeyStartPattern<T>(partitionKeyStartPattern, cancellationToken, amount, tableName);

            return DeleteEntities(entitiesToDel, cancellationToken, tableName);
        }

        /// <summary>
        /// Deletes entities in the table with the specified partition key start pattern, 
        /// in one or more batch transactions to the service for execution.
        /// The sub-operations contained in one batch will either succeed 
        /// or fail together as a transaction.
        /// </summary>
        /// <param name="partitionKeyStartPattern">The partition key start pattern that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation.</param>
        /// <param name="amount">Number of entities to delete</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse"/> 
        /// with information about each batch execution.</returns>
        public virtual List<AzStorageResponse<IReadOnlyList<Response>>> DeleteEntitiesByPartitionKeyStartPattern(
            string partitionKeyStartPattern,
            string tableName,
            CancellationToken cancellationToken = default,
            int amount = ConstProvider.DefaultTake)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrWhitespace(tableName, nameof(tableName), nameof(tableName));

            var entitiesToDel = QueryByPartitionKeyStartPattern<TableEntity>(partitionKeyStartPattern, cancellationToken, amount, tableName);

            return DeleteEntities(entitiesToDel, cancellationToken, tableName);
        }

        /// <summary>
        /// Deletes entities in the table with the specified partition key and row key start pattern, 
        /// in one or more batch transactions to the service for execution.
        /// The sub-operations contained in one batch will either succeed 
        /// or fail together as a transaction.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the entities.</param>
        /// <param name="rowKeyStartPattern">The row key start pattern that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <param name="amount">Number of entities to delete</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{IReadOnlyList{Response}}"/> 
        /// with information about each batch execution.</returns>
        public virtual List<AzStorageResponse<IReadOnlyList<Response>>> DeleteEntitiesByPartitionKeyRowKeyStartPattern<T>(
            string partitionKey,
            string rowKeyStartPattern,
            CancellationToken cancellationToken = default,
            string tableName = null,
            int amount = ConstProvider.DefaultTake) where T : class, ITableEntity, new()
        {
            var entitiesToDel = QueryByPartitionKeyRowKeyStartPattern<T>(partitionKey, rowKeyStartPattern, cancellationToken, amount, tableName);

            return DeleteEntities(entitiesToDel, cancellationToken, tableName);
        }

        /// <summary>
        /// Deletes entities in the table with the specified partition key and row key start pattern, 
        /// in one or more batch transactions to the service for execution.
        /// The sub-operations contained in one batch will either succeed 
        /// or fail together as a transaction.
        /// </summary>
        /// <param name="partitionKey">The partition key that identifies the entities.</param>
        /// <param name="rowKeyStartPattern">The row key start pattern that identifies the entities.</param>
        /// <param name="tableName">The table name to execute the operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="amount">Number of entities to delete</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{IReadOnlyList{Response}}"/> 
        /// with information about each batch execution.</returns>
        public virtual List<AzStorageResponse<IReadOnlyList<Response>>> DeleteEntitiesByPartitionKeyRowKeyStartPattern(
            string partitionKey,
            string rowKeyStartPattern,
            string tableName,
            CancellationToken cancellationToken = default,
            int amount = ConstProvider.DefaultTake)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrWhitespace(tableName, nameof(tableName), nameof(tableName));

            var entitiesToDel = QueryByPartitionKeyRowKeyStartPattern<TableEntity>(partitionKey, rowKeyStartPattern, cancellationToken, amount, tableName);

            return DeleteEntities(entitiesToDel, cancellationToken, tableName);
        }

        /// <summary>
        /// Deletes entities in one or more batch transactions to the service for execution.
        /// The sub-operations contained in one batch will either succeed 
        /// or fail together as a transaction.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entities">The entities to delete.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{IReadOnlyList{Response}}"/> 
        /// with information about each batch execution.</returns>
        public virtual List<AzStorageResponse<IReadOnlyList<Response>>> DeleteEntitiesTransactionally<T>(IEnumerable<T> entities,
            CancellationToken cancellationToken = default,
            string tableName = default) where T : class, ITableEntity, new()
        {
            ExThrower.ST_ThrowIfArgumentIsNull(entities, nameof(entities));

            var azTableTransactionStore = CreateAzTableTransactionStore();
            azTableTransactionStore.ClearNDelete(entities);

            return GetTableClient<T>(tableName).SubmitTransaction<AzTableTransactionStore>(
                azTableTransactionStore, cancellationToken);
        }

        /// <summary>
        /// Deletes all entities in the table, in one or more 
        /// batch transactions to the service for execution.
        /// The sub-operations contained in one batch will either succeed 
        /// or fail together as a transaction.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageRespons{IReadOnlyList{Response}}e"/> 
        /// with information about each batch execution.</returns>
        public virtual List<AzStorageResponse<IReadOnlyList<Response>>> ClearTable<T>(
            CancellationToken cancellationToken = default,
            string tableName = default) where T : class, ITableEntity, new()
        {
            var entitiesToDel = QueryAll<T>(cancellationToken: cancellationToken, tableName: tableName);

            return DeleteEntities(entitiesToDel, cancellationToken, tableName);
        }

        /// <summary>
        /// Deletes all entities in the table, in one or more 
        /// batch transactions to the service for execution.
        /// The sub-operations contained in one batch will either succeed 
        /// or fail together as a transaction.
        /// </summary>
        /// <param name="tableName">The table name to execute the operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{IReadOnlyList{Response}}"/> 
        /// with information about each batch execution.</returns>
        public virtual List<AzStorageResponse<IReadOnlyList<Response>>> ClearTable(
            string tableName,
            CancellationToken cancellationToken = default)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrWhitespace(tableName, nameof(tableName), nameof(tableName));

            var entitiesToDel = QueryAll<TableEntity>(cancellationToken: cancellationToken, tableName: tableName);

            return DeleteEntities(entitiesToDel, cancellationToken, tableName);
        }

        #endregion

        #region Delete async

        /// <summary>
        /// Deletes the specified table entity.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entity">The entity to delete.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse> DeleteEntityAsync<T>(T entity,
            CancellationToken cancellationToken = default,
            string tableName = default) where T : class, ITableEntity, new()
        {
            return await DeleteEntityAsync<T, AzStorageResponse>(entity,
                cancellationToken,
                tableName);
        }

        /// <summary>
        /// Deletes the specified table entity.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <typeparam name="TOut">A custom model type that inherits of <see cref="AzStorageResponse" /> or an instance of <see cref="AzStorageResponse"/>.</typeparam>
        /// <param name="entity">The entity to delete.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>The <typeparamref name="TOut"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<TOut> DeleteEntityAsync<T, TOut>(T entity,
            CancellationToken cancellationToken = default,
            string tableName = default) where T : class, ITableEntity, new() where TOut : AzStorageResponse, new()
        {
            ExThrower.ST_ThrowIfArgumentIsNull(entity, nameof(entity));

            return await DeleteEntityAsync<T, TOut>(entity.PartitionKey, entity.RowKey,
                entity.ETag,
                cancellationToken,
                tableName);
        }

        /// <summary>
        /// Deletes the specified table entity.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the table entity.</param>
        /// <param name="rowKey">The row key that identifies the table entity.</param>
        /// <param name="ifMatch">
        /// The If-Match value to be used for optimistic concurrency.
        /// If <see cref="ETag.All"/> is specified, the operation will be executed unconditionally.
        /// If the <see cref="ITableEntity.ETag"/> value is specified, the operation will fail with a status of 412 (Precondition Failed)
        /// if the <see cref="ETag"/> value of the entity in the table does not match.
        /// The default is to delete unconditionally.
        /// </param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse> DeleteEntityAsync<T>(string partitionKey, string rowKey,
            ETag ifMatch = default,
            CancellationToken cancellationToken = default,
            string tableName = default) where T : class, ITableEntity, new()
        {
            return await DeleteEntityAsync<T, AzStorageResponse>(partitionKey, rowKey,
                ifMatch,
                cancellationToken,
                tableName);
        }

        /// <summary>
        /// Deletes the specified table entity.
        /// </summary>
        /// <param name="partitionKey">The partition key that identifies the table entity.</param>
        /// <param name="rowKey">The row key that identifies the table entity.</param>
        /// <param name="tableName">The table name to execute the operation.</param>
        /// <param name="ifMatch">
        /// The If-Match value to be used for optimistic concurrency.
        /// If <see cref="ETag.All"/> is specified, the operation will be executed unconditionally.
        /// If the <see cref="ITableEntity.ETag"/> value is specified, the operation will fail with a status of 412 (Precondition Failed)
        /// if the <see cref="ETag"/> value of the entity in the table does not match.
        /// The default is to delete unconditionally.
        /// </param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<AzStorageResponse> DeleteEntityAsync(string partitionKey, string rowKey,
            string tableName,
            ETag ifMatch = default,
            CancellationToken cancellationToken = default)
        {
            return await DeleteEntityAsync<AzStorageResponse>(partitionKey, rowKey,
                tableName,
                ifMatch,
                cancellationToken);
        }

        /// <summary>
        /// Deletes the specified table entity.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <typeparam name="TOut">A custom model type that inherits of <see cref="AzStorageResponse" /> 
        /// or an instance of <see cref="AzStorageResponse"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the table entity.</param>
        /// <param name="rowKey">The row key that identifies the table entity.</param>
        /// <param name="ifMatch">
        /// The If-Match value to be used for optimistic concurrency.
        /// If <see cref="ETag.All"/> is specified, the operation will be executed unconditionally.
        /// If the <see cref="ITableEntity.ETag"/> value is specified, the operation will fail with a status of 412 (Precondition Failed)
        /// if the <see cref="ETag"/> value of the entity in the table does not match.
        /// The default is to delete unconditionally.
        /// </param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>The <typeparamref name="TOut"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<TOut> DeleteEntityAsync<T, TOut>(string partitionKey, string rowKey,
            ETag ifMatch = default,
            CancellationToken cancellationToken = default,
            string tableName = default) where T : class, ITableEntity, new() where TOut : AzStorageResponse, new()
        {
            return await FuncHelper.ExecuteAsync<string, string, ETag, CancellationToken, Response, TOut>(
                GetTableClient<T>(tableName).DeleteEntityAsync,
                partitionKey, rowKey, ifMatch, cancellationToken);
        }

        /// <summary>
        /// Deletes the specified table entity.
        /// </summary>
        /// <typeparam name="TOut">A custom model type that inherits of <see cref="AzStorageResponse" /> 
        /// or an instance of <see cref="AzStorageResponse"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the table entity.</param>
        /// <param name="rowKey">The row key that identifies the table entity.</param>
        /// <param name="tableName">The table name to execute the operation.</param>
        /// <param name="ifMatch">
        /// The If-Match value to be used for optimistic concurrency.
        /// If <see cref="ETag.All"/> is specified, the operation will be executed unconditionally.
        /// If the <see cref="ITableEntity.ETag"/> value is specified, the operation will fail with a status of 412 (Precondition Failed)
        /// if the <see cref="ETag"/> value of the entity in the table does not match.
        /// The default is to delete unconditionally.
        /// </param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>The <typeparamref name="TOut"/> indicating the result of the operation, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<TOut> DeleteEntityAsync<TOut>(string partitionKey, string rowKey,
            string tableName,
            ETag ifMatch = default,
            CancellationToken cancellationToken = default) where TOut : AzStorageResponse, new()
        {
            return await FuncHelper.ExecuteAsync<string, string, ETag, CancellationToken, Response, TOut>(
                GetTableClient(tableName).DeleteEntityAsync,
                partitionKey, rowKey, ifMatch, cancellationToken);
        }

        /// <summary>
        /// Deletes a Table Entities of type <typeparamref name="TIn"/> into the Table.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entities">The entities to add.</param>
        /// <param name="deleteEntityCancellationToken">A <see cref="CancellationToken"/> controlling the delete operation lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <param name="parallelOptions">Options that configure the operation of methods on the System.Threading.Tasks.Parallel</param>
        /// <returns>Number of entities deleted</returns>
        public virtual int DeleteEntitiesParallelForEach<T>(IEnumerable<T> entities,
            CancellationToken deleteEntityCancellationToken = default,
            string tableName = null,
            ParallelOptions parallelOptions = null) where T : class, ITableEntity, new()
        {
            return Helper.ExecuteParallelForEach(DeleteEntity<T, AzStorageResponse>, entities, tableName,
                ResponseValidator.ResponseSucceeded,
                deleteEntityCancellationToken,
                parallelOptions);
        }

        /// <summary>
        /// Deletes entities in one or more batch transactions to the service for execution.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entities">The entities to delete.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse{IReadOnlyList{Response}}"/> 
        /// with information about each batch execution, 
        /// that was created contained within a System.Threading.Tasks.Task object representing 
        /// the service response for the asynchronous operation.</returns>
        public virtual async Task<List<AzStorageResponse<IReadOnlyList<Response>>>> DeleteEntitiesTransactionallyAsync<T>(IEnumerable<T> entities,
            CancellationToken cancellationToken = default,
            string tableName = default) where T : class, ITableEntity, new()
        {
            ExThrower.ST_ThrowIfArgumentIsNull(entities, nameof(entities));

            var _azTableTransactionStore = new AzTableTransactionStore();
            _azTableTransactionStore.Delete(entities);

            return await GetTableClient<T>(tableName).SubmitTransactionAsync<AzTableTransactionStore>(
                _azTableTransactionStore, cancellationToken);
        }

        #endregion

        #region UpdateKeys

        /// <summary>
        /// Updates the partition key of the specified table entity of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the table entity.</param>
        /// <param name="rowKey">The row key that identifies the table entity.</param>
        /// <param name="newPartitionKey">The new partition key.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse UpdatePartitionKey<T>(
            string partitionKey, string rowKey,
            string newPartitionKey,
            CancellationToken cancellationToken = default,
            string tableName = default) where T : class, ITableEntity, new()
        {
            return UpdateKeys<T>(
                partitionKey,
                rowKey,
                newPartitionKey,
                rowKey,
                cancellationToken,
                tableName);
        }

        /// <summary>
        /// Updates the row key of the specified table entity of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the table entity.</param>
        /// <param name="rowKey">The row key that identifies the table entity.</param>
        /// <param name="newRowKey">The new row key</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse UpdateRowKey<T>(
            string partitionKey, string rowKey,
            string newRowKey,
            CancellationToken cancellationToken = default,
            string tableName = default) where T : class, ITableEntity, new()
        {
            return UpdateKeys<T>(
                partitionKey,
                rowKey,
                partitionKey,
                newRowKey,
                cancellationToken,
                tableName);
        }

        /// <summary>
        /// Updates the partition key and row key of the specified table entity of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> 
        /// or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the table entity.</param>
        /// <param name="rowKey">The row key that identifies the table entity.</param>
        /// <param name="newPartitionKey">The new partition key.</param>
        /// <param name="newRowKey">The new row key</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, 
        /// the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse UpdateKeys<T>(
            string partitionKey, string rowKey,
            string newPartitionKey, string newRowKey,
            CancellationToken cancellationToken = default,
            string tableName = default) where T : class, ITableEntity, new()
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmpty(partitionKey, nameof(partitionKey));
            ExThrower.ST_ThrowIfArgumentIsNullOrEmpty(rowKey, nameof(rowKey));

            ExThrower.ST_ThrowIfArgumentIsNullOrEmpty(newPartitionKey, nameof(newPartitionKey));
            ExThrower.ST_ThrowIfArgumentIsNullOrEmpty(newRowKey, nameof(newRowKey));

            if (newPartitionKey.Equals(partitionKey) && newRowKey.Equals(rowKey))
                ExThrower.ST_ThrowArgumentException(message: ErrorTextProvider.Current_keys_same_as_new_keys);

            CreateOrLoadTableClient<T>(tableName);

            var _getEntityResponse = GetEntity<T>(partitionKey, rowKey, cancellationToken, tableName);
            if (!ResponseValidator.ResponseSucceeded<AzStorageResponse<T>, T>(_getEntityResponse))
                return _getEntityResponse.InduceGenericLessResponse();

            var entity = _getEntityResponse.Value;
            entity.PartitionKey = newPartitionKey;
            entity.RowKey = newRowKey;

            var entityToDelete = new TableEntity { PartitionKey = partitionKey, RowKey = rowKey };
            if (partitionKey.Equals(newPartitionKey))
            {
                var azTableTransactionStore = CreateAzTableTransactionStore();
                azTableTransactionStore.Delete(entityToDelete);
                azTableTransactionStore.Add(entity);

                var submitTransactionResponse = FuncHelper.Execute<IEnumerable<TableTransactionAction>, CancellationToken, Response<IReadOnlyList<Response>>,
                    AzStorageResponse<IReadOnlyList<Response>>, IReadOnlyList<Response>>(
                    GetTableClient<T>(tableName).SubmitTransaction, azTableTransactionStore, cancellationToken);

                return submitTransactionResponse.InduceGenericLessResponse();
            }
            else
            {
                var _addEntityResponse = AddEntity(entity, cancellationToken, tableName);
                if (!ResponseValidator.ResponseSucceeded(_addEntityResponse))
                    return _addEntityResponse;

                var _deleteEntityResponse = DeleteEntity(entityToDelete, cancellationToken, tableName);
                if (!ResponseValidator.ResponseSucceeded(_deleteEntityResponse))
                    DeleteEntity(entity, cancellationToken, tableName);

                return _deleteEntityResponse;
            }
        }

        #endregion
    }
}

