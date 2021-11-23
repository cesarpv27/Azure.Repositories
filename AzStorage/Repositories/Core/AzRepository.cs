using AzCoreTools.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ExThrower = CoreTools.Throws.ExceptionThrower;

namespace AzStorage.Repositories.Core
{
    public abstract class AzRepository<Rtry> : IRepository where Rtry : AzStorageRetryOptions
    {
        public AzRepository(CreateResourcePolicy createResourcePolicy = CreateResourcePolicy.OnlyFirstTime,
            Rtry retryOptions = null)
        {
            CreateResourcePolicy = createResourcePolicy;
            RetryOptions = retryOptions;
        }

        #region Properties

        /// <summary>
        /// Default value is 'OnlyFirstTime'
        /// </summary>
        public virtual CreateResourcePolicy CreateResourcePolicy { get; protected set; }

        public virtual Rtry RetryOptions { get; protected set; }

        #endregion

        #region Protected methods

        protected virtual ClientOpt CreateClientOptions<ClientOpt>(ClientOpt clientOpt = default)
            where ClientOpt : Azure.Core.ClientOptions, new()
        {
            if (clientOpt == default)
                clientOpt = new ClientOpt();

            if (RetryOptions != default)
                RetryOptions.CopyTo(clientOpt.Retry);

            return clientOpt;
        }

        protected virtual bool TryCreateResource<TOut>(
            dynamic funcCreateResource,
            dynamic[] funcParams,
            ref bool isFirstTime,
            out TOut result) where TOut : class
        {
            if (CreateResourcePolicy == CreateResourcePolicy.Always
                || (CreateResourcePolicy == CreateResourcePolicy.OnlyFirstTime && isFirstTime))
            {
                ExThrower.ST_ThrowIfArgumentIsNull(funcCreateResource);

                result = funcCreateResource(funcParams);
                isFirstTime = false;

                return true;
            }

            result = null;
            isFirstTime = false;

            return false;
        }

        protected virtual bool TryCreateOrGetResource<TOut>(
            dynamic funcCreateResource,
            dynamic[] createResourceParams,
            dynamic funcGetResource,
            dynamic[] getResourceParams,
            Func<Exception, bool> funcExpectedException,
            ref bool isFirstTime,
            out TOut result) where TOut : class
        {
            try
            {
                return TryCreateResource(funcCreateResource, createResourceParams,
                    ref isFirstTime, out result);
            }
            catch (Exception e)
            {
                if (funcExpectedException != default && !funcExpectedException(e))
                    throw;
                //if (!BlobErrorManager.ExceptionContainsContainerAlreadyExistsAzError(e))
                //    throw;

                //result = true;
                //_blobContainerClientResponse = GetBlobServiceClient().GetBlobContainerClient(blobContainerName);

                ExThrower.ST_ThrowIfArgumentIsNull(funcGetResource);

                result = funcGetResource(getResourceParams);
                isFirstTime = false;
                return true;
            }
        }

        #endregion
    }
}
