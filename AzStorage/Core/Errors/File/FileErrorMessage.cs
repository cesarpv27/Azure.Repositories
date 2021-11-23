using System;
using System.Collections.Generic;
using System.Text;

namespace AzStorage.Core.Errors.File
{
    public class FileErrorMessage
    {
        public const string CannotDeleteFileOrDirectory = "The file or directory could not be deleted because it is in use by an SMB client.";
        public const string ClientCacheFlushDelay = "The specified resource state could not be flushed from an SMB client in the specified time.";
        public const string DeletePending = "The specified resource is marked for deletion by an SMB client.";
        public const string DirectoryNotEmpty = "The specified directory is not empty.";
        public const string FileLockConflict = "A portion of the specified file is locked by an SMB client.";
        public const string InvalidFileOrDirectoryPathName_PathTooLong = "File or directory path is too long.";
        public const string InvalidFileOrDirectoryPathName_PathTooManySubdir = "File or directory path has too many subdirectories.";
        public const string ParentNotFound = "The specified parent path does not exist.";
        public const string ReadOnlyAttribute = "The specified resource is read-only and cannot be modified at this time.";
        public const string ShareAlreadyExists = "The specified share already exists.";
        public const string ShareBeingDeleted = "The specified share is being deleted. Try operation later.";
        public const string ShareDisabled = "The specified share is disabled by the administrator.";
        public const string ShareNotFound = "The specified share does not exist.";
        public const string SharingViolation = "The specified resource may be in use by an SMB client.";
        public const string ShareSnapshotInProgress = "Another Share Snapshot operation is in progress.";
        public const string ShareSnapshotCountExceeded = "The total number of snapshots for the share is over the limit.";
        public const string ShareSnapshotOperationNotSupported = "The operation is not supported on a share snapshot.";
        public const string ShareHasSnapshots = "The share has snapshots and the operation requires no snapshots.";
        public const string ContainerQuotaDowngradeNotAllowed = "Cannot downgrade quota at this moment. Please check share properties for the next allowed quota downgrade time.";
    }
}
