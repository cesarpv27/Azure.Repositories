using System;
using System.Collections.Generic;
using System.Text;
using AzCoreTools.Core.Interfaces;
using AzCoreTools.Extensions;
using ExThrower = CoreTools.Throws.ExceptionThrower;

namespace AzStorage.Core.Errors
{
    public class AzErrorManager //: IAzErrorManager
    {
        public virtual AzError GetAzErrorFromException<TEx>(TEx exception) where TEx : Exception
        {
            ExThrower.ST_ThrowIfArgumentIsNull(exception, nameof(exception), nameof(exception));

            var httpStatusCode = exception.GetAzureHttpStatusCode();
            if (httpStatusCode == default)
                return default;

            var errorCode = exception.GetAzureErrorCode();
            var errorMessage = exception.GetAzureErrorMessage();

            return new AzError(errorCode, httpStatusCode.Value, errorMessage);
        }

        public virtual bool ExceptionContainsAzError<TEx>(TEx exception, AzError azError) where TEx : Exception
        {
            ExThrower.ST_ThrowIfArgumentIsNull(exception, nameof(exception), nameof(exception));
            ExThrower.ST_ThrowIfArgumentIsNull(azError, nameof(azError), nameof(azError));

            var exceptionAzError = GetAzErrorFromException(exception);
            if (exceptionAzError == default)
                return false;

            return azError.Equals(exceptionAzError);
        }
    }
}
