using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Azure.Core;
using Azure;
using System.Net;
using ExThrower = CoreTools.Throws.ExceptionThrower;
using AzCoreTools.Core.Validators;
using Azure.Data.Tables.Models;
using AzStorage.Core.Tables;
using AzCoreTools.Core;

namespace AzStorage.Repositories.Core
{
    public abstract class AzStorageRepository
    {
        #region Properties

        public virtual string ConnectionString { get; protected set; }
        public virtual AzTableRetryOptions AzTableRetryOptions { get; protected set; }
        /// <summary>
        /// Default value is 'OnlyFirstTime'
        /// </summary>
        public virtual OptionCreateResource OptionCreateResource { get; protected set; } = OptionCreateResource.OnlyFirstTime;

        protected virtual bool IsFirstTime { get; private set; } = true;

        #endregion

        #region Protected methods

        protected virtual void SetTrueToIsFirstTime()
        {
            IsFirstTime = true;
        }

        protected virtual void SetFalseToIsFirstTime()
        {
            IsFirstTime = false;
        }

        protected virtual async Task<KeyValuePair<bool, TOut>> TryCreateResourceAsync<TIn1, TIn2, TOut>(TIn1 param1, TIn2 param2,
            Func<TIn1, TIn2, Task<TOut>> funcCreateResourceAsync) where TOut : class
        {
            if (OptionCreateResource == OptionCreateResource.Always
                || (OptionCreateResource == OptionCreateResource.OnlyFirstTime && IsFirstTime))
            {
                var result = await funcCreateResourceAsync(param1, param2);
                SetFalseToIsFirstTime();

                return new KeyValuePair<bool, TOut>(true, result);
            }

            SetFalseToIsFirstTime();

            return new KeyValuePair<bool, TOut>(false, null);
        }

        protected virtual bool TryCreateResource<TIn1, TIn2, TOut>(TIn1 param1, TIn2 param2, out TOut result,
            Func<TIn1, TIn2, TOut> funcCreateResource) where TOut : class
        {
            if (OptionCreateResource == OptionCreateResource.Always
                || (OptionCreateResource == OptionCreateResource.OnlyFirstTime && IsFirstTime))
            {
                result = funcCreateResource(param1, param2);
                SetFalseToIsFirstTime();

                return true;
            }

            result = null;
            SetFalseToIsFirstTime();

            return false;
        }

        #endregion

        #region Throw methods

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
