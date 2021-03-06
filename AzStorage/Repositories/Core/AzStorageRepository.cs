using ExThrower = CoreTools.Throws.ExceptionThrower;
using AzCoreTools.Core;
using AzStorage.Core.Texting;
using System.Threading;

namespace AzStorage.Repositories.Core
{
    public abstract class AzStorageRepository<Rtry> : AzRepository<Rtry> where Rtry : AzStorageRetryOptions
    {
        protected AzStorageRepository() : base() { }

        public AzStorageRepository(CreateResourcePolicy createResourcePolicy, Rtry retryOptions)
            : base(createResourcePolicy, retryOptions)
        {
        }

        #region Properties

        /// <summary>
        /// A connection string includes the authentication information required for your
        /// application to access data in an Azure Storage account at runtime.
        /// </summary>
        public virtual string ConnectionString { get; protected set; }

        protected virtual bool IsFirstTimeResourceCreation { get; set; } = true;

        #endregion

        #region Protected methods

        protected virtual void SetTrueToIsFirstTime()
        {
            IsFirstTimeResourceCreation = true;
        }

        protected virtual AzStorageResponse<T> GetAzStorageResponseWithOperationCanceledMessage<T>()
        {
            return AzStorageResponse<T>.Create(ErrorTextProvider.Operation_canceled_by_cancellationToken);
        }
        
        protected virtual AzStorageResponse GetAzStorageResponseWithOperationCanceledMessage()
        {
            return AzStorageResponse.Create(ErrorTextProvider.Operation_canceled_by_cancellationToken);
        }

        #endregion

        #region Throws and validations

        protected virtual void ThrowIfInvalidConnectionString()
        {
            ThrowIfInvalidConnectionString(ConnectionString, nameof(ConnectionString));
        }

        protected virtual void ThrowIfInvalidConnectionString(string connectionString, string message = null)
        {
            if (string.IsNullOrEmpty(message))
                message = nameof(connectionString);

            if (!ValidateConnectionString(connectionString))
                ExThrower.ST_ThrowNullReferenceException(message);
        }

        protected virtual bool ValidateConnectionString(string connectionString)
        {
            return !string.IsNullOrEmpty(connectionString) && !string.IsNullOrWhiteSpace(connectionString);
        }

        protected virtual bool ValidateCancellationToken(CancellationToken cancellationToken)
        {
            return cancellationToken != default;
        }

        protected virtual bool IsCancellationRequested(CancellationToken cancellationToken)
        {
            return ValidateCancellationToken(cancellationToken) && cancellationToken.IsCancellationRequested;
        }

        #endregion
    }
}
