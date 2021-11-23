using System;
using System.Collections.Generic;
using System.Text;

namespace AzStorage.Core.Errors.Queue
{
    public class QueueErrorMessage
    {
        public const string InvalidMarker = "The specified marker is invalid.";
        public const string MessageNotFound = "The specified message does not exist.";
        public const string MessageTooLarge = "The message exceeds the maximum allowed size.";
        public const string PopReceiptMismatch = "The specified pop receipt did not match the pop receipt for a dequeued message.";
        public const string QueueAlreadyExists = "The specified queue already exists.";
        public const string QueueBeingDeleted = "The specified queue is being deleted.";
        public const string QueueDisabled = "The specified queue has been disabled by the administrator.";
        public const string QueueNotEmpty = "The specified queue is not empty.";
        public const string QueueNotFound = "The specified queue does not exist.";
    }
}
