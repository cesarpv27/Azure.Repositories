using System;
using System.Collections.Generic;
using System.Text;

namespace AzStorage.Core.Errors.Table
{
    public class TableErrorCode
    {
        public const string DuplicatePropertiesSpecified = nameof(DuplicatePropertiesSpecified);
        public const string EntityNotFound = nameof(EntityNotFound);
        public const string EntityAlreadyExists = nameof(EntityAlreadyExists);
        public const string EntityTooLarge = nameof(EntityTooLarge);
        public const string HostInformationNotPresent = nameof(HostInformationNotPresent);
        public const string InvalidDuplicateRow = nameof(InvalidDuplicateRow);
        public const string InvalidInput = nameof(InvalidInput);
        public const string InvalidValueType = nameof(InvalidValueType);
        public const string JsonFormatNotSupported = nameof(JsonFormatNotSupported);
        public const string MethodNotAllowed = nameof(MethodNotAllowed);
        public const string NotImplemented = nameof(NotImplemented);
        public const string OutOfRangeInput = nameof(OutOfRangeInput);
        public const string PropertiesNeedValue = nameof(PropertiesNeedValue);
        public const string PropertyNameInvalid = nameof(PropertyNameInvalid);
        public const string PropertyNameTooLong = nameof(PropertyNameTooLong);
        public const string PropertyValueTooLarge = nameof(PropertyValueTooLarge);
        public const string TableAlreadyExists = nameof(TableAlreadyExists);
        public const string TableBeingDeleted = nameof(TableBeingDeleted);
        public const string TableNotFound = nameof(TableNotFound);
        public const string TooManyProperties = nameof(TooManyProperties);
        public const string UpdateConditionNotSatisfied = nameof(UpdateConditionNotSatisfied);
        public const string XMethodIncorrectCount = nameof(XMethodIncorrectCount);
        public const string XMethodIncorrectValue = nameof(XMethodIncorrectValue);
        public const string XMethodNotUsingPost = nameof(XMethodNotUsingPost);
    }
}
