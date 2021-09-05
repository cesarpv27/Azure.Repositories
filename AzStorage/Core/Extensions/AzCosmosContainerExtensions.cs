using AzCoreTools.Core;
using AzCoreTools.Helpers;
using AzStorage.Core.Cosmos;
using AzStorage.Core.Tables;
using AzStorage.Core.Utilities;
using Azure;
using Azure.Data.Tables;
using CoreTools.Extensions;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExThrower = CoreTools.Throws.ExceptionThrower;

namespace AzStorage.Core.Extensions
{
    public static class AzCosmosContainerExtensions
    {
        private static TransactionalBatch BuildTransactionBatch<T, TRange>(this TransactionalBatch _transactionalBatch,
            TRange azTransactionRange,
            Func<T, string> funcGetId)
            where TRange : IEnumerable<TransactionAction<T>>
        {
            foreach (var elem in azTransactionRange)
                switch (elem.ActionType)
                {
                    case TransactionActionType.Add:
                        _transactionalBatch = _transactionalBatch.CreateItem(elem.Entity);
                        break;
                    case TransactionActionType.Get:
                        _transactionalBatch = _transactionalBatch.ReadItem(funcGetId(elem.Entity));
                        break;
                    case TransactionActionType.Update:
                        _transactionalBatch = _transactionalBatch.ReplaceItem(funcGetId(elem.Entity), elem.Entity);
                        break;
                    case TransactionActionType.Upsert:
                        _transactionalBatch = _transactionalBatch.UpsertItem(elem.Entity);
                        break;
                    case TransactionActionType.Delete:
                        _transactionalBatch = _transactionalBatch.DeleteItem(funcGetId(elem.Entity));
                        break;
                    case TransactionActionType.UpdateMerge:
                    case TransactionActionType.UpdateReplace:
                    case TransactionActionType.UpsertMerge:
                    case TransactionActionType.UpsertReplace:
                    default:
                        ExThrower.ST_ThrowNotImplementedException(elem.ActionType);
                        break;
                }

            return _transactionalBatch;
        }

        private static TransactionalBatch GenerateTransactionalBatch<T, TRange>(
            this Container _container,
            TRange azTransactionRange,
            Func<T, string> funcGetPartitionKey,
            Func<T, string> funcGetId)
            where TRange : IEnumerable<TransactionAction<T>>
        {
            var _transactionalBatch = _container.CreateTransactionalBatch(
                new PartitionKey(funcGetPartitionKey(azTransactionRange.First().Entity)));

            return _transactionalBatch.BuildTransactionBatch(azTransactionRange, funcGetId);
        }

        private static async Task<AzCosmosResponse<TransactionalBatchResponse>> LimitedSubmitTransactionAsync<T, TRange>(
            this Container _container,
            TRange azTransactionRange,
            Func<T, string> funcGetPartitionKey,
            Func<T, string> funcGetId = default,
            CancellationToken cancellationToken = default)
            where TRange : IEnumerable<TransactionAction<T>>
        {
            var _transactionalBatch = _container.GenerateTransactionalBatch(azTransactionRange, funcGetPartitionKey, funcGetId);

            return await CosmosFuncHelper.ExecuteTransactionalBatchAsync<CancellationToken, TransactionalBatchResponse,
                AzCosmosResponse<TransactionalBatchResponse>>(
                _transactionalBatch.ExecuteAsync, cancellationToken);
        }

        public static async Task<List<AzCosmosResponse<TransactionalBatchResponse>>> SubmitTransactionAsync<T, TStore>(
            this Container _container,
            TStore azCosmosTransactionStore,
            Func<T, string> funcGetPartitionKey,
            Func<T, string> funcGetId = default,
            CancellationToken cancellationToken = default)
            where TStore : AzCosmosTransactionStore<T>
        {
            ExThrower.ST_ThrowIfArgumentIsNull(azCosmosTransactionStore, nameof(azCosmosTransactionStore));

            var _responses = new List<AzCosmosResponse<TransactionalBatchResponse>>(azCosmosTransactionStore.Count);

            foreach (var range in azCosmosTransactionStore.GetRangeEnumerable())
                _responses.Add(await _container.LimitedSubmitTransactionAsync(range,
                    funcGetPartitionKey, funcGetId, cancellationToken));

            return _responses;
        }

    }
}
