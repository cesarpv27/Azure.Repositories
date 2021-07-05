using AzCoreTools.Utilities;
using Azure.Data.Tables;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using ExThrower = CoreTools.Throws.ExceptionThrower;
using CoreTools.Extensions;
using AzStorage.Core.Texting;

namespace AzStorage.Core.Tables
{
    public class AzTableTransactionStore : AzTableTransactionStore<ITableEntity>
    {
    }

    public class AzTableTransactionStore<T> : IEnumerable<TableTransactionAction> where T : ITableEntity
    {
        private int count;
        private int _version;
        private string dictionaryKey;
        private Dictionary<string, List<TableTransactionAction>> _transactionActions;
        protected virtual Dictionary<string, List<TableTransactionAction>> TransactionActions
        {
            get
            {
                if (_transactionActions == null)
                    _transactionActions = new Dictionary<string, List<TableTransactionAction>>();

                return _transactionActions;
            }
        }

        private string GenerateDictionaryKey(T entity, TableTransactionActionType _tableTransactionActionType)
        {
            return $"{entity.PartitionKey}";
        }

        private void CreateStoreAction(T entity, TableTransactionActionType _tableTransactionActionType)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(entity, nameof(entity));

            count++;
            _version++;
            dictionaryKey = GenerateDictionaryKey(entity, _tableTransactionActionType);

            if (TransactionActions.TryGetValue(dictionaryKey, out List<TableTransactionAction> _tranActions))
            {
                if (_tranActions.Count < 100)
                {
                    _tranActions.AddTableTransactionAction(entity, _tableTransactionActionType);
                    return;
                }
                TransactionActions.Add($"{dictionaryKey}_{count}", _tranActions);
            }

            CreateOrReplaceNewListTableTransactionAction(dictionaryKey, entity, _tableTransactionActionType);
        }

        private void CreateOrReplaceNewListTableTransactionAction(string dictionaryKey,
            T entity, 
            TableTransactionActionType _tableTransactionActionType)
        {
            var newList = new List<TableTransactionAction>(100);
            newList.AddTableTransactionAction(entity, _tableTransactionActionType);

            TransactionActions.AddOrReplace(dictionaryKey, newList);
        }

        private void CreateStoreActions(IEnumerable<T> entities, TableTransactionActionType _tableTransactionActionType)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(entities, nameof(entities));

            foreach (var ent in entities)
                CreateStoreAction(ent, _tableTransactionActionType);
        }

        public virtual void Clear()
        {
            TransactionActions.Clear();
            _version++;
        }

        public virtual int Count
        {
            get
            {
                return TransactionActions.Count;
            }
        }

        private void ActionByModificationMode<TIn>(TIn param, TableUpdateMode mode, Action<TIn> merge, Action<TIn> replace)
        {
            switch (mode)
            {
                case TableUpdateMode.Merge:
                    merge(param);
                    break;
                case TableUpdateMode.Replace:
                    replace(param);
                    break;
                default:
                    ExThrower.ST_ThrowArgumentOutOfRangeException(mode);
                    break;
            }
        }

        #region Add

        public virtual void Add(T entity)
        {
            CreateStoreAction(entity, TableTransactionActionType.Add);
        }

        public virtual void Add(IEnumerable<T> entities)
        {
            CreateStoreActions(entities, TableTransactionActionType.Add);
        }

        public virtual void ClearNAdd(IEnumerable<T> entities)
        {
            Clear();
            Add(entities);
        }

        #endregion

        #region Update

        public virtual void Update(T entity, TableUpdateMode mode)
        {
            ActionByModificationMode(entity, mode, UpdateMerge, UpdateReplace);
        }

        public virtual void Update(IEnumerable<T> entities, TableUpdateMode mode)
        {
            ActionByModificationMode(entities, mode, UpdateMerge, UpdateReplace);
        }

        public virtual void ClearNUpdate(IEnumerable<T> entities, TableUpdateMode mode)
        {
            Clear();
            Update(entities, mode);
        }

        #endregion

        #region UpdateMerge

        protected virtual void UpdateMerge(T entity)
        {
            CreateStoreAction(entity, TableTransactionActionType.UpdateMerge);
        }

        protected virtual void UpdateMerge(IEnumerable<T> entities)
        {
            CreateStoreActions(entities, TableTransactionActionType.UpdateMerge);
        }

        #endregion

        #region UpdateReplace

        protected virtual void UpdateReplace(T entity)
        {
            CreateStoreAction(entity, TableTransactionActionType.UpdateReplace);
        }

        protected virtual void UpdateReplace(IEnumerable<T> entities)
        {
            CreateStoreActions(entities, TableTransactionActionType.UpdateReplace);
        }

        #endregion

        #region Delete

        public virtual void Delete(T entity)
        {
            CreateStoreAction(entity, TableTransactionActionType.Delete);
        }

        public virtual void Delete(IEnumerable<T> entities)
        {
            CreateStoreActions(entities, TableTransactionActionType.Delete);
        }

        public virtual void ClearNDelete(IEnumerable<T> entities)
        {
            Clear();
            Delete(entities);
        }

        #endregion

        #region Upsert

        public virtual void Upsert(T entity, TableUpdateMode mode)
        {
            ActionByModificationMode(entity, mode, UpsertMerge, UpsertReplace);
        }

        public virtual void Upsert(IEnumerable<T> entities, TableUpdateMode mode)
        {
            ActionByModificationMode(entities, mode, UpsertMerge, UpsertReplace);
        }

        public virtual void ClearNUpsert(IEnumerable<T> entities, TableUpdateMode mode)
        {
            Clear();
            Upsert(entities, mode);
        }

        #endregion

        #region UpsertMerge

        protected virtual void UpsertMerge(T entity)
        {
            CreateStoreAction(entity, TableTransactionActionType.UpsertMerge);
        }

        protected virtual void UpsertMerge(IEnumerable<T> entities)
        {
            CreateStoreActions(entities, TableTransactionActionType.UpsertMerge);
        }

        #endregion

        #region UpsertReplace

        protected virtual void UpsertReplace(T entity)
        {
            CreateStoreAction(entity, TableTransactionActionType.UpsertReplace);
        }

        protected virtual void UpsertReplace(IEnumerable<T> entities)
        {
            CreateStoreActions(entities, TableTransactionActionType.UpsertReplace);
        }

        #endregion

        #region IEnumerable

        public IEnumerator<TableTransactionAction> GetEnumerator()
        {
            return TransactionActions.GetEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return TransactionActions.GetEnumerable().GetEnumerator();
        }

        #endregion

        #region GetRangeEnumerable

        public IEnumerable<List<TableTransactionAction>> GetRangeEnumerable()
        {
            return TransactionActions.Values;
        }

        #endregion
    }

    static class Extensions
    {
        public static void AddTableTransactionAction<T>(
            this List<TableTransactionAction> @this,
            T entity, 
            TableTransactionActionType _tableTransactionActionType)
            where T : ITableEntity
        {
            @this.Add(new TableTransactionAction(_tableTransactionActionType, entity, entity.ETag));
        }

        public static IEnumerable<TableTransactionAction> GetEnumerable(
            this Dictionary<string, List<TableTransactionAction>> @this)
        {
            foreach (var _list in @this.Values)
                foreach (var _tranAction in _list)
                    yield return _tranAction;
        }
    }
}
