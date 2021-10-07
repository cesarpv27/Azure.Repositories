using System;
using System.Collections.Generic;
using System.Text;

namespace AzStorage.Core.Texting
{
    public static class ErrorTextProvider
    {
        public const string Unexpected_value = "Unexpected value.";
        public const string Error_retrieving_entities = "Error retrieving entities.";
        public const string Version_of_collection_modified = "Version of collection has been modified.";
        public const string Operation_not_allowed = "Operation not allowed.";
        public const string Current_keys_same_as_new_keys = "Current keys are the same as the new keys.";
        public const string Can_not_load_create_table = "Can not load or create table.";
        public const string Can_not_load_create_queue = "Can not load or create queue.";
        public const string Invalid_KeyValuePair_key_null_empty_whitespaces = "Invalid element in collection, some KeyValuePair contains key equals null, empty or whitespace.";
        public const string Invalid_operation_message_defined_twice = "Invalid operation, the message has been defined twice.";
        public const string Invalid_operation_message_not_defined = "Invalid operation, the message has not been defined.";

        public static string receiptMetadata_is_null(string receiptMetadataParamName = null)
        {
            if (string.IsNullOrEmpty(receiptMetadataParamName))
                receiptMetadataParamName = "'receiptMetadata'";

            return receiptMetadataParamName + " is null";
        }

        public static string receiptMetadata_MessageId_is_null_or_empty(
            string receiptMetadataMessageIdParamName = null)
        {
            if (string.IsNullOrEmpty(receiptMetadataMessageIdParamName))
                receiptMetadataMessageIdParamName = "'receiptMetadata.MessageId'";

            return receiptMetadataMessageIdParamName + " is null or empty";
        }

        public static string receiptMetadata_MessageId_is_null_or_whitespace(
            string receiptMetadataMessageIdParamName = null)
        {
            if (string.IsNullOrEmpty(receiptMetadataMessageIdParamName))
                receiptMetadataMessageIdParamName = "'receiptMetadata.MessageId'";

            return receiptMetadataMessageIdParamName + " is null or whitespace";
        }

        public static string receiptMetadata_PopReceipt_is_null_or_empty(
            string receiptMetadataPopReceiptParamName = null)
        {
            if (string.IsNullOrEmpty(receiptMetadataPopReceiptParamName))
                receiptMetadataPopReceiptParamName = "'receiptMetadata.PopReceipt'";

            return receiptMetadataPopReceiptParamName + " is null or empty";
        }

        public static string receiptMetadata_PopReceipt_is_null_or_whitespace(
            string receiptMetadataPopReceiptParamName = null)
        {
            if (string.IsNullOrEmpty(receiptMetadataPopReceiptParamName))
                receiptMetadataPopReceiptParamName = "'receiptMetadata.PopReceipt'";

            return receiptMetadataPopReceiptParamName + " is null or whitespace";
        }
    }
}
