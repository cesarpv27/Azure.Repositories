using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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
using AzStorage.Core.Tables;
using AzCoreTools.Utilities;
using AzCoreTools.Helpers;
using AzStorage.Core.Extensions;
using AzCoreTools.Extensions;

namespace AzStorage.Repositories
{
    public partial class AzTableRepository
    {
        #region Add

        /// <summary>
        /// Adds a Table Entity of type <typeparamref name="TIn"/> into the Table.
        /// </summary>
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entity">The entity to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation.</returns>
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
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <typeparam name="TOut">A custom model type that inherits of <see cref="AzStorageResponse" /> or an instance of <see cref="AzStorageResponse"/>.</typeparam>
        /// <param name="entity">The entity to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>The <typeparamref name="TOut"/> indicating the result of the operation.</returns>
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
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entities">The entities to add.</param>
        /// <param name="addEntityCancellationToken">A <see cref="CancellationToken"/> controlling the add operation lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
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
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entities">The entities to add.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse"/> with information about each batch execution.</returns>
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
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the table entity.</param>
        /// <param name="rowKey">The row key that identifies the table entity.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse"/> indicating the result of the operation.</returns>
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
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse"/> containing a collection of entity models serialized as type <typeparamref name="T"/>.</returns>
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
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse"/> containing a collection of entity models serialized as type <typeparamref name="T"/>.</returns>
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
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="startPattern">The partition key start pattern that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse"/> containing a collection of entity models serialized as type <typeparamref name="T"/>.</returns>
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
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the table entity.</param>
        /// <param name="rowKey">The row key that identifies the table entity.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse"/> indicating the result of the operation.</returns>
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
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the entities.</param>
        /// <param name="rowKeyStartPattern">The row key start pattern that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse"/> containing a collection of entity models serialized as type <typeparamref name="T"/>.</returns>
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
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKeyStartPattern">The partition key start pattern that identifies the entities.</param>
        /// <param name="rowKeyStartPattern">The row key start pattern that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse"/> containing a collection of entity models serialized as type <typeparamref name="T"/>.</returns>
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
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="timeStampFrom">The start timestamp that identifies the entities.</param>
        /// <param name="timeStampTo">The end timestamp that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse"/> containing a collection of entity models serialized as type <typeparamref name="T"/>.</returns>
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
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="partitionKey">The partition key that identifies the entities.</param>
        /// <param name="timeStampFrom">The start timestamp that identifies the entities.</param>
        /// <param name="timeStampTo">The end timestamp that identifies the entities.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="take">Amount of entities to take in response. This value must be greater than zero.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A <see cref="AzStorageResponse"/> containing a collection of entity models serialized as type <typeparamref name="T"/>.</returns>
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
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entity">The entity to update.</param>
        /// <param name="mode">Determines the behavior of the Update operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation.</returns>
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
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <typeparam name="TOut">A custom model type that inherits of <see cref="AzStorageResponse" /> or an instance of <see cref="AzStorageResponse"/>.</typeparam>
        /// <param name="entity">The entity to update.</param>
        /// <param name="mode">Determines the behavior of the Update operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>The <typeparamref name="TOut"/> indicating the result of the operation.</returns>
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
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse"/> with information about each batch execution.</returns>
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
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
        /// <returns>The <typeparamref name="TOut"/> indicating the result of the operation.</returns>
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
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entity">The entity to delete.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation.</returns>
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
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>The <typeparamref name="TOut"/> indicating the result of the operation.</returns>
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
        /// <returns>The <see cref="AzStorageResponse"/> indicating the result of the operation.</returns>
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
        /// <typeparam name="TIn">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entities">The entities to add.</param>
        /// <param name="deleteEntityCancellationToken">A <see cref="CancellationToken"/> controlling the delete operation lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="TIn"/> will be taken as table name.</param>
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
        /// <typeparam name="T">A custom model type that implements <see cref="ITableEntity" /> or an instance of <see cref="TableEntity"/>.</typeparam>
        /// <param name="entities">The entities to delete.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
        /// <param name="tableName">The table name to execute the operation. If <paramref name="tableName"/> is null or empty, the name of type <typeparamref name="T"/> will be taken as table name.</param>
        /// <returns>A collection containing responses of type <see cref="AzStorageResponse"/> with information about each batch execution.</returns>
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
    }
}
