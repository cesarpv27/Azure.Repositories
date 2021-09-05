using AzCoreTools.Core;
using AzCoreTools.Helpers;
using AzStorage.Core.Tables;
using Azure;
using Azure.Data.Tables;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExThrower = CoreTools.Throws.ExceptionThrower;

namespace AzStorage.Core.Extensions
{
    public static class AzTableClientExtensions
    {
        private static AzStorageResponse<IReadOnlyList<Response>> LimitedSubmitTransaction<TRange>(
            this TableClient tableClient,
            TRange azTableTransactionStore,
            CancellationToken cancellationToken = default)
            where TRange : IEnumerable<TableTransactionAction>
        {
            return FuncHelper.Execute<IEnumerable<TableTransactionAction>, CancellationToken, Response<IReadOnlyList<Response>>,
                AzStorageResponse<IReadOnlyList<Response>>, IReadOnlyList<Response>>(
                tableClient.SubmitTransaction, azTableTransactionStore, cancellationToken);
        }

        public static List<AzStorageResponse<IReadOnlyList<Response>>> SubmitTransaction<TStore>(
            this TableClient tableClient,
            TStore azTableTransactionStore,
            CancellationToken cancellationToken = default)
            where TStore : AzTableTransactionStore
        {
            ExThrower.ST_ThrowIfArgumentIsNull(azTableTransactionStore, nameof(azTableTransactionStore));

            var _responses = new List<AzStorageResponse<IReadOnlyList<Response>>>(azTableTransactionStore.Count);

            foreach (var range in azTableTransactionStore.GetRangeEnumerable())
                _responses.Add(tableClient.LimitedSubmitTransaction<IEnumerable<TableTransactionAction>>(range, 
                    cancellationToken));

            return _responses;
        }

        #region Async

        private static async Task<AzStorageResponse<IReadOnlyList<Response>>> LimitedSubmitTransactionAsyn<TTAct>(
            this TableClient tableClient,
            TTAct azTableTransactionStore,
            CancellationToken cancellationToken = default)
            where TTAct : IEnumerable<TableTransactionAction>
        {
            return await FuncHelper.ExecuteAsync<IEnumerable<TableTransactionAction>, CancellationToken, Response<IReadOnlyList<Response>>,
                AzStorageResponse<IReadOnlyList<Response>>, IReadOnlyList<Response>>(
                tableClient.SubmitTransactionAsync, azTableTransactionStore, cancellationToken);
        }

        public static async Task<List<AzStorageResponse<IReadOnlyList<Response>>>> SubmitTransactionAsync<TStore>(
            this TableClient tableClient,
            TStore azTableTransactionStore,
            CancellationToken cancellationToken = default)
            where TStore : AzTableTransactionStore
        {
            ExThrower.ST_ThrowIfArgumentIsNull(azTableTransactionStore, nameof(azTableTransactionStore));

            var _responses = new List<AzStorageResponse<IReadOnlyList<Response>>>(azTableTransactionStore.Count);

            foreach (var range in azTableTransactionStore.GetRangeEnumerable())
                _responses.Add(await tableClient.LimitedSubmitTransactionAsyn<IEnumerable<TableTransactionAction>>(range, 
                    cancellationToken));

            return _responses;
        }

        #endregion
    }
}
