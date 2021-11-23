using System;
using System.Collections.Generic;
using System.Text;

namespace AzStorage.Core.Errors.File
{
    public class FileErrorCode
    {
        public const string CannotDeleteFileOrDirectory = nameof(CannotDeleteFileOrDirectory);
        public const string ClientCacheFlushDelay = nameof(ClientCacheFlushDelay);
        public const string DeletePending = nameof(DeletePending);
        public const string DirectoryNotEmpty = nameof(DirectoryNotEmpty);
        public const string FileLockConflict = nameof(FileLockConflict);
        public const string InvalidFileOrDirectoryPathName = nameof(InvalidFileOrDirectoryPathName);
        public const string ParentNotFound = nameof(ParentNotFound);
        public const string ReadOnlyAttribute = nameof(ReadOnlyAttribute);
        public const string ShareAlreadyExists = nameof(ShareAlreadyExists);
        public const string ShareBeingDeleted = nameof(ShareBeingDeleted);
        public const string ShareDisabled = nameof(ShareDisabled);
        public const string ShareNotFound = nameof(ShareNotFound);
        public const string SharingViolation = nameof(SharingViolation);
        public const string ShareSnapshotInProgress = nameof(ShareSnapshotInProgress);
        public const string ShareSnapshotCountExceeded = nameof(ShareSnapshotCountExceeded);
        public const string ShareSnapshotOperationNotSupported = nameof(ShareSnapshotOperationNotSupported);
        public const string ShareHasSnapshots = nameof(ShareHasSnapshots);
        public const string ContainerQuotaDowngradeNotAllowed = nameof(ContainerQuotaDowngradeNotAllowed);
    }
}
