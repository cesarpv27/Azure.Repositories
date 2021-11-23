using System;
using System.Collections.Generic;
using System.Text;

namespace AzStorage.Core.Errors.Table
{
    public class TableErrorMessage
    {
        public const string DuplicatePropertiesSpecified = "A property is specified more than one time.";
        public const string EntityNotFound = "The specified entity does not exist.";
        public const string EntityAlreadyExists = "The specified entity already exists.";
        public const string EntityTooLarge = "The entity is larger than the maximum size permitted.";
        public const string HostInformationNotPresent = "The required host information is not present in the request. You must send a non-empty Host header or include the absolute URI in the request line.";
        public const string InvalidDuplicateRow = "A command with RowKey '{0}' is already present in the batch. An entity can appear only once in a batch.";
        public const string InvalidInput_BatchExceedsMaximum = "The batch request operation exceeds the maximum 100 changes per change set.";
        public const string InvalidInput_CannotSpecifiedForPUT = "If-None-Match HTTP header cannot be specified for PUT operations.";
        public const string InvalidInput_CannotSpecifiedIfDoesNotHaveEtag = "If-Match or If-None-Match headers cannot be specified if the target type does not have etag properties defined.";
        public const string InvalidInput_CannotSpecifiedSameTime = "Both If-Match and If-None-Match HTTP headers cannot be specified at the same time. Please specify either one of the headers or none of them.";
        public const string InvalidInput_CannotSpecifiedForDELETE = "If-None-Match HTTP header cannot be specified for DELETE operations.";
        public const string InvalidInput_EtagValueNotValid = "The etag value '{0}' specified in one of the request headers is not valid. Please make sure only one etag value is specified and is valid.";
        public const string InvalidValueType = "The value specified is invalid.";
        public const string JsonFormatNotSupported = "JSON format is not supported.";
        public const string MethodNotAllowed = "The requested method is not allowed on the specified resource.";
        public const string NotImplemented = "The requested operation is not implemented on the specified resource.";
        public const string OutOfRangeInput = "The '{0}' parameter of value '{1}' is out of range.";
        public const string PropertiesNeedValue = "Values have not been specified for all properties in the entity.";
        public const string PropertyNameInvalid = "The property name is invalid.";
        public const string PropertyNameTooLong = "The property name exceeds the maximum allowed length.";
        public const string PropertyValueTooLarge = "The property value is larger than the maximum size permitted.";
        public const string TableAlreadyExists = "The table specified already exists.";
        public const string TableBeingDeleted = "The specified table is being deleted.";
        public const string TableNotFound = "The table specified does not exist.";
        public const string TooManyProperties = "The entity contains more properties than allowed.";
        public const string UpdateConditionNotSatisfied = "The update condition specified in the request was not satisfied.";
        public const string XMethodIncorrectCount = "More than one X-HTTP-Method is specified.";
        public const string XMethodIncorrectValue = "The specified X-HTTP-Method is invalid.";
        public const string XMethodNotUsingPost = "The request uses X-HTTP-Method with an HTTP verb other than POST.";
    }
}
