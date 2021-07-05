using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AzStorage.Core;
using AzStorage.Core.Helpers;
using AzCoreTools.Core;
using AzCoreTools.Core.Validators;
using AzStorage.Repositories.Core;
using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using CoreTools.Extensions;
using CoreTools.Helpers;
using ExThrower = CoreTools.Throws.ExceptionThrower;
using AzCoreTools.Extensions;
using AzStorage.Core.Texting;
using AzStorage.Core.Tables;
using AzStorage.Core.Extensions;
using AzCoreTools.Helpers;
using AzCoreTools.Utilities;
using CoreTools.Throws;

namespace AzStorage.Repositories
{
    public partial class AzTableRepository : AzStorageRepository
    {
        protected AzTableRepository() { }

        public AzTableRepository(string connectionString,
            OptionCreateResource optionCreateTableResource = OptionCreateResource.OnlyFirstTime,
            AzTableRetryOptions retryOptions = null)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(connectionString, nameof(connectionString));

            ConnectionString = connectionString;
            OptionCreateResource = optionCreateTableResource;
            AzTableRetryOptions = retryOptions;
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

        protected TableClient _TableClient;
        protected virtual TableClient GetTableClient<T>(string tableName = default) where T : class, ITableEntity, new()
        {
            return GetTableClient(GetTableName<T>(tableName));
        }

        protected virtual TableClient GetTableClient(string tableName)
        {
            var response = LoadTableClient(tableName);
            if (response != null && !ResponseValidator.ResponseSucceeded(response.GetRawResponse()))
                ExThrower.ST_ThrowInvalidOperationException(ErrorTextProvider.Can_not_load_create_table);

            return _TableClient;
        }

        //private AzTableTransactionStore _AzTableTransactionStore;
        //private AzTableTransactionStore AzTableTransactionStore
        //{
        //    get
        //    {
        //        if (_AzTableTransactionStore == null)
        //            _AzTableTransactionStore = new AzTableTransactionStore();

        //        return _AzTableTransactionStore;
        //    }
        //}

        protected AzTableTransactionStore CreateAzTableTransactionStore()
        {
            return new AzTableTransactionStore(); ;
        }

        #endregion

        #region Protected methods

        protected virtual TableServiceClient CreateTableServiceClient()
        {
            ThrowIfConnectionStringIsInvalid();

            return new TableServiceClient(ConnectionString, CreateTableClientOptions());
        }

        protected virtual TableClient CreateTableClient(string tableName)
        {
            ThrowIfConnectionStringIsInvalid();

            return new TableClient(ConnectionString, tableName, CreateTableClientOptions());
        }

        protected virtual TableClientOptions CreateTableClientOptions()
        {
            var _tableClientOptions = new TableClientOptions();
            if (AzTableRetryOptions != null)
                AzTableRetryOptions.CopyTo(_tableClientOptions.Retry);

            return _tableClientOptions;
        }

        protected virtual string GetTableName<T>(string tableName = default) where T : class
        {
            if (string.IsNullOrEmpty(tableName))
                tableName = typeof(T).Name;

            return tableName;
        }

        protected virtual Response<TableItem> LoadTableClient(string tableName)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmpty(tableName, nameof(tableName));

            if (_TableClient == null || !tableName.Equals(_TableClient.Name))
                SetTrueToIsFirstTime();

            Response<TableItem> response;
            var result = TryCreateResource(tableName, default(CancellationToken), out response,
                TableServiceClient.CreateTableIfNotExists);

            if (result && ResponseValidator.CreateResourceResponseSucceeded(response))
                _TableClient = CreateTableClient(tableName);

            return response;
        }

        protected virtual Response<TableItem> LoadTableClient<T>(string tableName = default) where T : class
        {
            return LoadTableClient(GetTableName<T>(tableName));
        }

        protected List<AzStorageResponse<IReadOnlyList<Response>>> DeleteEntities<T>(AzStorageResponse<List<T>> response,
            CancellationToken cancellationToken = default,
            string tableName = default) where T : class, ITableEntity, new()
        {
            if (!response.Succeeded)
                ExThrower.ST_ThrowInvalidOperationException($"{nameof(response)} opperations must be successful");

            if (response.Value == null)
                ExThrower.ST_ThrowApplicationException(ErrorTextProvider.Error_retrieving_entities);

            return DeleteEntitiesTransactionally(response.Value, cancellationToken, tableName);
        }

        #endregion

        #region Add

        /// <summary>
        /// Adds a Table Entity of type <typeparamref name="TIn"/> into the Table.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entity">The entity to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
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
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <typeparam name="TOut">A custom model type that inherits of <see cref="AzStorageResponse" /> or an instance of <see cref="AzStorageResponse"/>.</typeparam>
        /// <param name="entity">The entity to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
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
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entities">The entities to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
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

        #region Get

        /// <summary>
        /// Gets the specified table entity of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the table entity.</param>
        /// <param name="rowKey">The row key that identifies the table entity.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse"/> indicating the result of the operation.</returns>
        public virtual AzStorageResponse<T> GetEntity<T>(string partitionKey, string rowKey,
            CancellationToken cancellationToken = default,
            string tableName = default) where T : class, ITableEntity, new()
        {
            return FuncHelper.Execute<string, string, IEnumerable<string>, CancellationToken, Response<T>, AzStorageResponse<T>, T>(
                GetTableClient<T>(tableName).GetEntity<T>,
                partitionKey, rowKey, default, cancellationToken);
        }

        #endregion

        #region Query

        /// <summary>
        /// Queries all entities in the table.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse"/> containing a collection of entity models serialized as type <typeparamref name="T"/>.</returns>
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
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse"/> containing a collection of entity models serialized as type <typeparamref name="T"/>.</returns>
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
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="startPattern">The partition key start pattern that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse"/> containing a collection of entity models serialized as type <typeparamref name="T"/>.</returns>
        public virtual AzStorageResponse<List<T>> QueryByPartitionKeyStartPattern<T>(
            string startPattern,
            CancellationToken cancellationToken = default,
            int take = int.MaxValue,
            string tableName = default) where T : class, ITableEntity, new()
        {
            return FuncHelper.Execute<string, CancellationToken, int, AzStorageResponse<List<T>>,  AzStorageResponse<List<T>>, List<T>>(
                GetTableClient<T>(tableName).QueryByPartitionKeyStartPattern<T>,
                startPattern, cancellationToken, take);
        }

        /// <summary>
        /// Gets the specified table entity of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the table entity.</param>
        /// <param name="rowKey">The row key that identifies the table entity.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse"/> indicating the result of the operation.</returns>
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
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the entities.</param>
        /// <param name="rowKeyStartPattern">The row key start pattern that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse"/> containing a collection of entity models serialized as type <typeparamref name="T"/>.</returns>
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
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKeyStartPattern">The partition key start pattern that identifies the entities.</param>
        /// <param name="rowKeyStartPattern">The row key start pattern that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse"/> containing a collection of entity models serialized as type <typeparamref name="T"/>.</returns>
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
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="timeStampFrom">The start timestamp that identifies the entities.</param>
        /// <param name="timeStampTo">The end timestamp that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse"/> containing a collection of entity models serialized as type <typeparamref name="T"/>.</returns>
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
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the entities.</param>
        /// <param name="timeStampFrom">The start timestamp that identifies the entities.</param>
        /// <param name="timeStampTo">The end timestamp that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse"/> containing a collection of entity models serialized as type <typeparamref name="T"/>.</returns>
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

        #region Update

        /// <summary>
        /// Updates the specified table entity of type <typeparamref name="TIn"/>, if it exists.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Replace"/>, the entity will be replaced.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Merge"/>, the property values present in the
        /// <paramref name="entity"/> will be merged with the existing entity.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entity">The entity to update.</param>
        /// <param name="mode">Determines the behavior of the Update operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
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
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <typeparam name="TOut">A custom model type that inherits of <see cref="AzStorageResponse" /> or an instance of <see cref="AzStorageResponse"/>.</typeparam>
        /// <param name="entity">The entity to update.</param>
        /// <param name="mode">Determines the behavior of the Update operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
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
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entities">The entities to update.</param>
        /// <param name="mode">Determines the behavior of the Update operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse"/> with information about each batch execution.</returns>
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

        #region Upsert

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
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
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
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
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
        /// Replaces entities in one or more batch transactions to the service for execution, if they exists. Creates the entity if it does not exist.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Replace"/> and the entity exists, the entity will be replaced.
        /// If the <paramref name="mode"/> is <see cref="TableUpdateMode.Merge"/> and the entity exists, the property values present in the
        /// <paramref name="entities"/> will be merged with the existing entity.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entities">The entities to upsert.</param>
        /// <param name="mode">Determines the behavior of the update operation when the entity already exists in the table.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse"/> with information about each batch execution.</returns>
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

        #region Delete

        /// <summary>
        /// Deletes the specified table entity.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entity">The entity to delete.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
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
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <typeparam name="TOut">A custom model type that inherits of <see cref="AzStorageResponse" /> or an instance of <see cref="AzStorageResponse"/>.</typeparam>
        /// <param name="entity">The entity to delete.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
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
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
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
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
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
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <typeparam name="TOut">A custom model type that inherits of <see cref="AzStorageResponse" /> or an instance of <see cref="AzStorageResponse"/>.</typeparam>
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
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
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
        /// <typeparam name="TOut">A custom model type that inherits of <see cref="AzStorageResponse" /> or an instance of <see cref="AzStorageResponse"/>.</typeparam>
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
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(tableName, nameof(tableName));

            return FuncHelper.Execute<string, string, ETag, CancellationToken, Response, TOut>(
                GetTableClient(tableName).DeleteEntity, 
                partitionKey, rowKey, AzStorageHelper.GetValidETag(ifMatch), cancellationToken);
        }

        /// <summary>
        /// Deletes entities in the table with the specified partition key, in one or more batch transactions to the service for execution.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <param name="amount">Number of entities to delete</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse"/> with information about each batch execution.</returns>
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
        /// Deletes entities in the table with the specified partition key, in one or more batch transactions to the service for execution.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <param name="partitionKey">The partition key that identifies the entities.</param>
        /// <param name="tableName">The table name to execute the operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="amount">Number of entities to delete</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse"/> with information about each batch execution.</returns>
        public virtual List<AzStorageResponse<IReadOnlyList<Response>>> DeleteEntitiesByPartitionKey(
            string partitionKey,
            string tableName,
            CancellationToken cancellationToken = default,
            int amount = ConstProvider.DefaultTake)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(tableName, nameof(tableName));

            var entitiesToDel = QueryByPartitionKey<TableEntity>(partitionKey, cancellationToken, amount, tableName);

            return DeleteEntities(entitiesToDel, cancellationToken, tableName);
        }

        /// <summary>
        /// Deletes entities in the table with the specified partition key start pattern, in one or more batch transactions to the service for execution.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKeyStartPattern">The partition key start pattern that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <param name="amount">Number of entities to delete</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse"/> with information about each batch execution.</returns>
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
        /// Deletes entities in the table with the specified partition key start pattern, in one or more batch transactions to the service for execution.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <param name="partitionKeyStartPattern">The partition key start pattern that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation.</param>
        /// <param name="amount">Number of entities to delete</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse"/> with information about each batch execution.</returns>
        public virtual List<AzStorageResponse<IReadOnlyList<Response>>> DeleteEntitiesByPartitionKeyStartPattern(
            string partitionKeyStartPattern,
            string tableName,
            CancellationToken cancellationToken = default,
            int amount = ConstProvider.DefaultTake)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(tableName, nameof(tableName));

            var entitiesToDel = QueryByPartitionKeyStartPattern<TableEntity>(partitionKeyStartPattern, cancellationToken, amount, tableName);

            return DeleteEntities(entitiesToDel, cancellationToken, tableName);
        }

        /// <summary>
        /// Deletes entities in the table with the specified partition key and row key start pattern, in one or more batch transactions to the service for execution.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the entities.</param>
        /// <param name="rowKeyStartPattern">The row key start pattern that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <param name="amount">Number of entities to delete</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse"/> with information about each batch execution.</returns>
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
        /// Deletes entities in the table with the specified partition key and row key start pattern, in one or more batch transactions to the service for execution.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <param name="partitionKey">The partition key that identifies the entities.</param>
        /// <param name="rowKeyStartPattern">The row key start pattern that identifies the entities.</param>
        /// <param name="tableName">The table name to execute the operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="amount">Number of entities to delete</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse"/> with information about each batch execution.</returns>
        public virtual List<AzStorageResponse<IReadOnlyList<Response>>> DeleteEntitiesByPartitionKeyRowKeyStartPattern(
            string partitionKey,
            string rowKeyStartPattern,
            string tableName,
            CancellationToken cancellationToken = default,
            int amount = ConstProvider.DefaultTake)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(tableName, nameof(tableName));

            var entitiesToDel = QueryByPartitionKeyRowKeyStartPattern<TableEntity>(partitionKey, rowKeyStartPattern, cancellationToken, amount, tableName);

            return DeleteEntities(entitiesToDel, cancellationToken, tableName);
        }

        /// <summary>
        /// Deletes entities in one or more batch transactions to the service for execution.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entities">The entities to delete.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse"/> with information about each batch execution.</returns>
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
        /// Deletes all entities in the table, in one or more batch transactions to the service for execution.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse"/> with information about each batch execution.</returns>
        public virtual List<AzStorageResponse<IReadOnlyList<Response>>> ClearTable<T>(
            CancellationToken cancellationToken = default,
            string tableName = default) where T : class, ITableEntity, new()
        {
            var entitiesToDel = QueryAll<T>(cancellationToken: cancellationToken, tableName: tableName);

            return DeleteEntities(entitiesToDel, cancellationToken, tableName);
        }

        /// <summary>
        /// Deletes all entities in the table, in one or more batch transactions to the service for execution.
        /// The sub-operations contained in one batch will either succeed or fail together as a transaction.
        /// </summary>
        /// <param name="tableName">The table name to execute the operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse"/> with information about each batch execution.</returns>
        public virtual List<AzStorageResponse<IReadOnlyList<Response>>> ClearTable(
            string tableName,
            CancellationToken cancellationToken = default)
        {
            ExThrower.ST_ThrowIfArgumentIsNullOrEmptyOrWhitespace(tableName, nameof(tableName));

            var entitiesToDel = QueryAll<TableEntity>(cancellationToken: cancellationToken, tableName: tableName);

            return DeleteEntities(entitiesToDel, cancellationToken, tableName);
        }

        #endregion

        #region UpdateKeys

        /// <summary>
        /// Updates the partition key of the specified table entity of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the table entity.</param>
        /// <param name="rowKey">The row key that identifies the table entity.</param>
        /// <param name="newPartitionKey">The new partition key.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
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
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the table entity.</param>
        /// <param name="rowKey">The row key that identifies the table entity.</param>
        /// <param name="newRowKey">The new row key</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
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
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the table entity.</param>
        /// <param name="rowKey">The row key that identifies the table entity.</param>
        /// <param name="newPartitionKey">The new partition key.</param>
        /// <param name="newRowKey">The new row key</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
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

            LoadTableClient<T>(tableName);

            var _getEntityResponse = GetEntity<T>(partitionKey, rowKey, cancellationToken, tableName);
            if (!ResponseValidator.ResponseSucceeded(_getEntityResponse))
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

