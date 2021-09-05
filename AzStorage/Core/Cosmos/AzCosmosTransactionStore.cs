using System;
using System.Collections.Generic;
using System.Text;
using AzStorage.Core.Utilities;
using Azure.Data.Tables;
using ExThrower = CoreTools.Throws.ExceptionThrower;

namespace AzStorage.Core.Cosmos
{
    public class AzCosmosTransactionStore<T> : AzTransactionStore<T, TransactionAction<T>>
    {
        public AzCosmosTransactionStore(Func<T, string> funcGetPartitionKey) : base(funcGetPartitionKey,
            (entity, _transactionActionType) => new TransactionAction<T>(_transactionActionType, entity))
        { }

        #region Update

        public new virtual void Update(T entity)
        {
            base.Update(entity);
        }

        public new virtual void Update(IEnumerable<T> entities)
        {
            base.Update(entities);
        }

        public new virtual void ClearNUpdate(IEnumerable<T> entities)
        {
            base.ClearNUpdate(entities);
        }

        #endregion

        #region Upsert

        public new virtual void Upsert(T entity)
        {
            base.Upsert(entity);
        }

        public new virtual void Upsert(IEnumerable<T> entities)
        {
            base.Upsert(entities);
        }

        public new virtual void ClearNUpsert(IEnumerable<T> entities)
        {
            base.ClearNUpsert(entities);
        }

        #endregion
    }
}
