using CoreTools.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using ExThrower = CoreTools.Throws.ExceptionThrower;

namespace AzStorage.Core.Utilities
{
    public abstract class AzTransactionStore<T, TranAct> : IEnumerable<TranAct>
    {
        public AzTransactionStore() { }
        public AzTransactionStore(Func<T, string> funcGetPartitionKey, 
            Func<T, TransactionActionType, TranAct> funcCreateTransactionAction)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(funcGetPartitionKey, nameof(funcGetPartitionKey), nameof(funcGetPartitionKey));
            ExThrower.ST_ThrowIfArgumentIsNull(funcCreateTransactionAction, nameof(funcCreateTransactionAction), nameof(funcCreateTransactionAction));

            this.funcGetPartitionKey = funcGetPartitionKey;
            this.funcCreateTransactionAction = funcCreateTransactionAction;
        }

        Func<T, string> funcGetPartitionKey;
        Func<T, TransactionActionType, TranAct> funcCreateTransactionAction;
        private int count;
        private string dictionaryKey;
        private Dictionary<string, List<TranAct>> _transactionActions;
        protected virtual Dictionary<string, List<TranAct>> TransactionActions
        {
            get
            {
                if (_transactionActions == null)
                    _transactionActions = new Dictionary<string, List<TranAct>>();

                return _transactionActions;
            }
        }

        private string GenerateDictionaryKey(T entity, TransactionActionType _transactionActionType)
        {
            return funcGetPartitionKey(entity);
        }

        private void CreateStoreAction(T entity, TransactionActionType _transactionActionType)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(entity, nameof(entity));

            count++;
            dictionaryKey = GenerateDictionaryKey(entity, _transactionActionType);

            if (TransactionActions.TryGetValue(dictionaryKey, out List<TranAct> _tranActions))
            {
                if (_tranActions.Count < 100)
                {
                    _tranActions.AddTableTransactionAction(entity, _transactionActionType, funcCreateTransactionAction);
                    return;
                }
                TransactionActions.Add($"{dictionaryKey}_{count}", _tranActions);
            }

            CreateOrReplaceNewListTableTransactionAction(dictionaryKey, entity, _transactionActionType);
        }

        private void CreateStoreActions(IEnumerable<T> entities, TransactionActionType _transactionActionType)
        {
            ExThrower.ST_ThrowIfArgumentIsNull(entities, nameof(entities));

            foreach (var ent in entities)
                CreateStoreAction(ent, _transactionActionType);
        }

        private void CreateOrReplaceNewListTableTransactionAction(string dictionaryKey,
            T entity,
            TransactionActionType _tableTransactionActionType)
        {
            var newList = new List<TranAct>(100);
            newList.AddTableTransactionAction(entity, _tableTransactionActionType, funcCreateTransactionAction);

            TransactionActions.AddOrReplace(dictionaryKey, newList);
        }

        public virtual void Clear()
        {
            TransactionActions.Clear();
        }

        public virtual int Count
        {
            get
            {
                return TransactionActions.Count;
            }
        }

        private void ApplyActionByMode<TIn>(TIn param, ActionMode mode, Action<TIn> merge, Action<TIn> replace)
        {
            switch (mode)
            {
                case ActionMode.Merge:
                    merge(param);
                    break;
                case ActionMode.Replace:
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
            CreateStoreAction(entity, TransactionActionType.Add);
        }

        public virtual void Add(IEnumerable<T> entities)
        {
            CreateStoreActions(entities, TransactionActionType.Add);
        }

        public virtual void ClearNAdd(IEnumerable<T> entities)
        {
            Clear();
            Add(entities);
        }

        #endregion

        #region Update

        protected virtual void Update(T entity)
        {
            CreateStoreAction(entity, TransactionActionType.Update);
        }

        protected virtual void Update(IEnumerable<T> entities)
        {
            CreateStoreActions(entities, TransactionActionType.Update);
        }

        protected virtual void ClearNUpdate(IEnumerable<T> entities)
        {
            Clear();
            Update(entities);
        }

        protected virtual void Update(T entity, ActionMode mode)
        {
            ApplyActionByMode(entity, mode, UpdateMerge, UpdateReplace);
        }

        protected virtual void Update(IEnumerable<T> entities, ActionMode mode)
        {
            ApplyActionByMode(entities, mode, UpdateMerge, UpdateReplace);
        }

        protected virtual void ClearNUpdate(IEnumerable<T> entities, ActionMode mode)
        {
            Clear();
            Update(entities, mode);
        }

        #region UpdateMerge

        protected virtual void UpdateMerge(T entity)
        {
            CreateStoreAction(entity, TransactionActionType.UpdateMerge);
        }

        protected virtual void UpdateMerge(IEnumerable<T> entities)
        {
            CreateStoreActions(entities, TransactionActionType.UpdateMerge);
        }

        #endregion

        #region UpdateReplace

        protected virtual void UpdateReplace(T entity)
        {
            CreateStoreAction(entity, TransactionActionType.UpdateReplace);
        }

        protected virtual void UpdateReplace(IEnumerable<T> entities)
        {
            CreateStoreActions(entities, TransactionActionType.UpdateReplace);
        }

        #endregion

        #endregion

        #region Upsert

        protected virtual void Upsert(T entity)
        {
            CreateStoreAction(entity, TransactionActionType.Upsert);
        }

        protected virtual void Upsert(IEnumerable<T> entities)
        {
            CreateStoreActions(entities, TransactionActionType.Upsert);
        }

        protected virtual void ClearNUpsert(IEnumerable<T> entities)
        {
            Clear();
            Upsert(entities);
        }

        protected virtual void Upsert(T entity, ActionMode mode)
        {
            ApplyActionByMode(entity, mode, UpsertMerge, UpsertReplace);
        }

        protected virtual void Upsert(IEnumerable<T> entities, ActionMode mode)
        {
            ApplyActionByMode(entities, mode, UpsertMerge, UpsertReplace);
        }

        public virtual void ClearNUpsert(IEnumerable<T> entities, ActionMode mode)
        {
            Clear();
            Upsert(entities, mode);
        }

        #region UpsertMerge

        protected virtual void UpsertMerge(T entity)
        {
            CreateStoreAction(entity, TransactionActionType.UpsertMerge);
        }

        protected virtual void UpsertMerge(IEnumerable<T> entities)
        {
            CreateStoreActions(entities, TransactionActionType.UpsertMerge);
        }

        #endregion

        #region UpsertReplace

        protected virtual void UpsertReplace(T entity)
        {
            CreateStoreAction(entity, TransactionActionType.UpsertReplace);
        }

        protected virtual void UpsertReplace(IEnumerable<T> entities)
        {
            CreateStoreActions(entities, TransactionActionType.UpsertReplace);
        }

        #endregion

        #endregion

        #region Delete

        public virtual void Delete(T entity)
        {
            CreateStoreAction(entity, TransactionActionType.Delete);
        }

        public virtual void Delete(IEnumerable<T> entities)
        {
            CreateStoreActions(entities, TransactionActionType.Delete);
        }

        public virtual void ClearNDelete(IEnumerable<T> entities)
        {
            Clear();
            Delete(entities);
        }

        #endregion

        #region IEnumerable

        public IEnumerator<TranAct> GetEnumerator()
        {
            return TransactionActions.GetEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return TransactionActions.GetEnumerable().GetEnumerator();
        }

        #endregion

        #region GetRangeEnumerable

        public IEnumerable<List<TranAct>> GetRangeEnumerable()
        {
            return TransactionActions.Values;
        }

        #endregion
    }

    static class Extensions
    {
        public static void AddTableTransactionAction<T, TranAct>(
            this List<TranAct> @this,
            T entity,
            TransactionActionType _transactionActionType,
            Func<T, TransactionActionType, TranAct> createTransactionAction)
        {
            @this.Add(createTransactionAction(entity, _transactionActionType));
        }

        public static IEnumerable<TranAct> GetEnumerable<TranAct>(
            this Dictionary<string, List<TranAct>> @this)
        {
            foreach (var _list in @this.Values)
                foreach (var _tranAction in _list)
                    yield return _tranAction;
        }
    }
}
