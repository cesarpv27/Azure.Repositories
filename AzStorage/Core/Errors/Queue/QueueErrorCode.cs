using System;
using System.Collections.Generic;
using System.Text;

namespace AzStorage.Core.Errors.Queue
{
    public class QueueErrorCode
    {
        public const string InvalidMarker = nameof(InvalidMarker);
        public const string MessageNotFound = nameof(MessageNotFound);
        public const string MessageTooLarge = nameof(MessageTooLarge);
        public const string PopReceiptMismatch = nameof(PopReceiptMismatch);
        public const string QueueAlreadyExists = nameof(QueueAlreadyExists);
        public const string QueueBeingDeleted = nameof(QueueBeingDeleted);
        public const string QueueDisabled = nameof(QueueDisabled);
        public const string QueueNotEmpty = nameof(QueueNotEmpty);
        public const string QueueNotFound = nameof(QueueNotFound);
    }
}
