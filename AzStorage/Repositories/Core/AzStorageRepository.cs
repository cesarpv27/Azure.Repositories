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

        protected virtual void ThrowIfConnectionStringIsInvalid()
        {
            if (!ValidateConnectionString(ConnectionString))
                ExThrower.ST_ThrowNullReferenceException(nameof(ConnectionString));
        }

        protected virtual bool ValidateConnectionString(string connectionString)
        {
            return !string.IsNullOrEmpty(connectionString) && !string.IsNullOrWhiteSpace(connectionString);
        }

        #endregion
    }
}
