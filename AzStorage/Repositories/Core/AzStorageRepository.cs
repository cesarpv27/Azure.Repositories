using ExThrower = CoreTools.Throws.ExceptionThrower;
using AzCoreTools.Core;

namespace AzStorage.Repositories.Core
{
    public abstract class AzStorageRepository<Rtry> : AzRepository<Rtry> where Rtry : AzStorageRetryOptions
    {
        protected AzStorageRepository() : base() { }

        public AzStorageRepository(CreateResourcePolicy createTableResource, Rtry retryOptions)
            : base(createTableResource, retryOptions)
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

        #endregion
    }
}
