using AzStorage.Core.Utilities;
using Azure.Data.Tables;
using System.Collections.Generic;
using ExThrower = CoreTools.Throws.ExceptionThrower;

namespace AzStorage.Core.Tables
{
    public class AzTableTransactionStore : AzTableTransactionStore<ITableEntity>
    {
    }

    public class AzTableTransactionStore<T> : AzTransactionStore<T, TableTransactionAction> where T : ITableEntity
    {
        public AzTableTransactionStore() : base(entity => entity.PartitionKey,
            (entity, _transactionActionType) => new TableTransactionAction(_transactionActionType.Transform(), entity, entity.ETag))
        { }

        #region Update

        public virtual void Update(T entity, TableUpdateMode mode)
        {
            base.Update(entity, mode.Transform());
        }

        public virtual void Update(IEnumerable<T> entities, TableUpdateMode mode)
        {
            base.Update(entities, mode.Transform());
        }

        public virtual void ClearNUpdate(IEnumerable<T> entities, TableUpdateMode mode)
        {
            base.ClearNUpdate(entities, mode.Transform());
        }

        #endregion

        #region Upsert

        public virtual void Upsert(T entity, TableUpdateMode mode)
        {
            base.Upsert(entity, mode.Transform());
        }

        public virtual void Upsert(IEnumerable<T> entities, TableUpdateMode mode)
        {
            base.Upsert(entities, mode.Transform());
        }

        public virtual void ClearNUpsert(IEnumerable<T> entities, TableUpdateMode mode)
        {
            base.ClearNUpsert(entities, mode.Transform());
        }

        #endregion
    }

    static class Extensions
    {
        public static TableTransactionActionType Transform(this TransactionActionType tableTransactionActionType)
        {
            switch (tableTransactionActionType)
            {
                case TransactionActionType.Add:
                    return TableTransactionActionType.Add;
                case TransactionActionType.UpdateMerge:
                    return TableTransactionActionType.UpdateMerge;
                case TransactionActionType.UpdateReplace:
                    return TableTransactionActionType.UpdateReplace;
                case TransactionActionType.Delete:
                    return TableTransactionActionType.Delete;
                case TransactionActionType.UpsertMerge:
                    return TableTransactionActionType.UpsertMerge;
                case TransactionActionType.UpsertReplace:
                    return TableTransactionActionType.UpsertReplace;
                default:
                    ExThrower.ST_ThrowArgumentOutOfRangeException(tableTransactionActionType);
                    return TableTransactionActionType.Add;// Mock return
            }
        }
        
        public static TransactionActionType Transform(this TableTransactionActionType tableTransactionActionType)
        {
            switch (tableTransactionActionType)
            {
                case TableTransactionActionType.Add:
                    return TransactionActionType.Add;
                case TableTransactionActionType.UpdateMerge:
                    return TransactionActionType.UpdateMerge;
                case TableTransactionActionType.UpdateReplace:
                    return TransactionActionType.UpdateReplace;
                case TableTransactionActionType.Delete:
                    return TransactionActionType.Delete;
                case TableTransactionActionType.UpsertMerge:
                    return TransactionActionType.UpsertMerge;
                case TableTransactionActionType.UpsertReplace:
                    return TransactionActionType.UpsertReplace;
                default:
                    ExThrower.ST_ThrowArgumentOutOfRangeException(tableTransactionActionType);
                    return TransactionActionType.Add;// Mock return
            }
        }

        public static ActionMode Transform(this TableUpdateMode tableUpdateMode)
        {
            switch (tableUpdateMode)
            {
                case TableUpdateMode.Merge:
                    return ActionMode.Merge;
                case TableUpdateMode.Replace:
                    return ActionMode.Replace;
                default:
                    ExThrower.ST_ThrowArgumentOutOfRangeException(tableUpdateMode);
                    return ActionMode.Merge;// Mock return
            }
        }
    }
}

