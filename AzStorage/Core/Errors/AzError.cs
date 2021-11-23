using System;
using System.Collections.Generic;
using System.Net;

namespace AzStorage.Core.Errors
{
    public class AzError
    {
        protected AzError() { }

        public AzError(string errorCode, HttpStatusCode httpStatusCode, string errorMessage)
        {
            ErrorCode = errorCode;
            HttpStatusCode = httpStatusCode;
            ErrorMessage = errorMessage;
        }

        public virtual string ErrorCode { get; protected set; }
        public virtual HttpStatusCode HttpStatusCode { get; protected set; }
        public virtual string ErrorMessage { get; protected set; }

        public override bool Equals(object obj)
        {
            if (!(obj is AzError azError))
                return false;
                
            return Equals(azError);
        }

        public bool Equals(AzError azError)
        {
            if (azError == null)
                return false;

            return string.Compare(ErrorCode, azError.ErrorCode) == 0
                && HttpStatusCode == azError.HttpStatusCode
                && string.Compare(ErrorMessage, azError.ErrorMessage) == 0;
        }

        public override int GetHashCode()
        {
            return string.Concat(ErrorCode, HttpStatusCode, ErrorMessage).GetHashCode();
        }

        public static bool operator ==(AzError azError1, AzError azError2)
        {
            if (ReferenceEquals(azError1, default))
                return ReferenceEquals(azError2, default);
            
            return azError1.Equals(azError2);
        }

        public static bool operator !=(AzError azError1, AzError azError2)
        {
            if (ReferenceEquals(azError1, default))
                return !ReferenceEquals(azError2, default);

            return !azError1.Equals(azError2);
        }
    }
}
