
namespace AzStorage.Core.Utilities
{
    /// <summary>
    /// Defines an transaction action to be included as part of a batch operation.
    /// </summary>
    /// <typeparam name="T">The entity type of the operation.</typeparam>
    public class TransactionAction<T>
    {
        public TransactionAction(TransactionActionType actionType, T entity)
        {
            ActionType = actionType;
            Entity = entity;
        }

        /// <summary>
        /// The operation type to be applied to the entity.
        /// </summary>
        public TransactionActionType ActionType { get; }

        /// <summary>
        /// The entity to which the batch operation will be applied.
        /// </summary>
        public virtual T Entity { get; }
    }
}
