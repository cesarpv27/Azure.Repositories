using System;
using System.Collections.Generic;
using System.Text;
using AzStorage.Core.Errors;
using System.Net;
using ExThrower = CoreTools.Throws.ExceptionThrower;

namespace AzStorage.Core.Errors.Blob
{
    public class BlobErrorManager : AzErrorManager
    {
        protected virtual AzError GenerateContainerAlreadyExistsAzError()
        {
            return new AzError(
                BlobErrorCode.ContainerAlreadyExists,
                HttpStatusCode.Conflict,
                BlobErrorMessage.ContainerAlreadyExists);
        }

        private AzError _ContainerAlreadyExistsAzError;
        public virtual AzError ContainerAlreadyExistsAzError
        {
            get
            {
                if (_ContainerAlreadyExistsAzError == null)
                    _ContainerAlreadyExistsAzError = GenerateContainerAlreadyExistsAzError();

                return _ContainerAlreadyExistsAzError;
            }
        }

        public virtual bool ExceptionContainsContainerAlreadyExistsAzError<TEx>(TEx exception) where TEx : Exception
        {
            ExThrower.ST_ThrowIfArgumentIsNull(exception, nameof(exception), nameof(exception));

            return ExceptionContainsAzError(exception, ContainerAlreadyExistsAzError);
        }
    }
}
