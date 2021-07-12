using AzCoreTools.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AzStorage.Repositories.Core
{
    public abstract class AzRepository<Rtry> : IRepository where Rtry : AzStorageRetryOptions
    {
        public AzRepository(CreateResourcePolicy createTableResource = CreateResourcePolicy.OnlyFirstTime,
            Rtry retryOptions = null)
        {
            CreateResourcePolicy = createTableResource;
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

        //protected virtual void SetFalseToIsFirstTime()
        //{
        //    IsFirstTime = false;
        //}

        //protected virtual async Task<KeyValuePair<bool, TOut>> TryCreateResourceAsync<TIn1, TIn2, TOut>(
        //    TIn1 param1, 
        //    TIn2 param2,
        //    Func<TIn1, TIn2, Task<TOut>> funcCreateResourceAsync) where TOut : class
        //{
        //    if (CreateResourcePolicy == CreateResourcePolicy.Always
        //        || (CreateResourcePolicy == CreateResourcePolicy.OnlyFirstTime && IsFirstTime))
        //    {
        //        var result = await funcCreateResourceAsync(param1, param2);
        //        SetFalseToIsFirstTime();

        //        return new KeyValuePair<bool, TOut>(true, result);
        //    }

        //    SetFalseToIsFirstTime();

        //    return new KeyValuePair<bool, TOut>(false, null);
        //}

        protected virtual bool TryCreateResource<TOut>(
            dynamic funcCreateResource,
            dynamic[] funcParams,
            ref bool isFirstTime,
            out TOut result) where TOut : class
        {
            if (CreateResourcePolicy == CreateResourcePolicy.Always
                || (CreateResourcePolicy == CreateResourcePolicy.OnlyFirstTime && isFirstTime))
            {
                result = funcCreateResource(funcParams);
                isFirstTime = false;

                return true;
            }

            result = null;
            isFirstTime = false;

            return false;
        }

        protected virtual bool TryCreateResource<TIn1, TIn2, TOut>(
            TIn1 param1,
            TIn2 param2,
            ref bool isFirstTime,
            out TOut result,
            Func<TIn1, TIn2, TOut> funcCreateResource) where TOut : class
        {
            if (CreateResourcePolicy == CreateResourcePolicy.Always
                || (CreateResourcePolicy == CreateResourcePolicy.OnlyFirstTime && isFirstTime))
            {
                result = funcCreateResource(param1, param2);
                isFirstTime = false;

                return true;
            }

            result = null;
            isFirstTime = false;

            return false;
        }

        #endregion
    }
}
