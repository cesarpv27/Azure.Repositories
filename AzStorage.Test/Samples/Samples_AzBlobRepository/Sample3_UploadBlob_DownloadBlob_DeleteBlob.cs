using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using System.Threading;
using Xunit;
using Azure.Storage.Blobs.Models;
using CoreTools.Extensions;

namespace AzStorage.Test.Samples.Samples_AzBlobRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample3_UploadBlob_DownloadBlob_DeleteBlob
    {
        [Fact, TestPriority(310)]
        public void UploadBlob_DownloadBlob_DeleteBlobTest()
        {
            // Arrange
            var blobStream = AzBlobUnitTestHelper.SampleTextStream;
            bool overwrite = false;
            CancellationToken cancellationToken = default;
            string blobContainerName = AzBlobUnitTestHelper.GetRandomBlobContainerNameFromDefault();
            string blobName = AzBlobUnitTestHelper.GetRandomBlobNameFromDefault();

            var snapshotsOption = DeleteSnapshotsOption.None;

            // Act
            var _uploadToBlobResponseAct = AzBlobUnitTestHelper.UploadBlob(blobStream, overwrite, cancellationToken,
                blobContainerName, blobName);
            var _downloadBlobResponseAct = AzBlobUnitTestHelper.DownloadBlob(default, cancellationToken,
                blobContainerName, blobName);
            var _deleteBlobDeleteBlobContainerResponseAct = AzBlobUnitTestHelper.DeleteBlobDeleteBlobContainer(snapshotsOption, 
                default, cancellationToken, blobContainerName, blobName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_uploadToBlobResponseAct);
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_downloadBlobResponseAct);
            UnitTestHelper.AssertExpectedSuccessfulResponses(_deleteBlobDeleteBlobContainerResponseAct);
        }
        
        [Fact, TestPriority(312)]
        public void UploadBlob_DownloadBlobBinaryData_DeleteBlobTest()
        {
            // Arrange
            var blobStream = AzBlobUnitTestHelper.SampleTextStream;
            bool overwrite = false;
            CancellationToken cancellationToken = default;
            string blobContainerName = AzBlobUnitTestHelper.GetRandomBlobContainerNameFromDefault();
            string blobName = AzBlobUnitTestHelper.GetRandomBlobNameFromDefault();

            var snapshotsOption = DeleteSnapshotsOption.None;

            // Act
            var _uploadToBlobResponseAct = AzBlobUnitTestHelper.UploadBlob(blobStream, overwrite, cancellationToken,
                blobContainerName, blobName);
            var _downloadBlobBinaryDataResponseAct = AzBlobUnitTestHelper.DownloadBlobBinaryData(default, cancellationToken,
                blobContainerName, blobName);
            var _deleteBlobDeleteBlobContainerResponseAct = AzBlobUnitTestHelper.DeleteBlobDeleteBlobContainer(snapshotsOption,
                default, cancellationToken, blobContainerName, blobName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_uploadToBlobResponseAct);
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_downloadBlobBinaryDataResponseAct);
            UnitTestHelper.AssertExpectedSuccessfulResponses(_deleteBlobDeleteBlobContainerResponseAct);
        }

        #region Async

        [Fact, TestPriority(350)]
        public void UploadBlob_DownloadBlobAsync_DeleteBlobAsyncTest()
        {
            // Arrange
            var blobStream = AzBlobUnitTestHelper.SampleTextStream;
            bool overwrite = false;
            CancellationToken cancellationToken = default;
            string blobContainerName = AzBlobUnitTestHelper.GetRandomBlobContainerNameFromDefault();
            string blobName = AzBlobUnitTestHelper.GetRandomBlobNameFromDefault();

            var snapshotsOption = DeleteSnapshotsOption.None;

            // Act
            var _uploadToBlobResponseAct = AzBlobUnitTestHelper.UploadBlob(blobStream, overwrite, cancellationToken,
                blobContainerName, blobName);
            var _downloadBlobAsyncResponseAct = AzBlobUnitTestHelper.DownloadBlobAsync(default, cancellationToken,
                blobContainerName, blobName).WaitAndUnwrapException();
            var _deleteBlobDeleteBlobContainerAsyncResponseAct = AzBlobUnitTestHelper.DeleteBlobDeleteBlobContainerAsync(snapshotsOption,
                default, cancellationToken, blobContainerName, blobName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_uploadToBlobResponseAct);
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_downloadBlobAsyncResponseAct);
            UnitTestHelper.AssertExpectedSuccessfulResponses(_deleteBlobDeleteBlobContainerAsyncResponseAct);
        }

        [Fact, TestPriority(352)]
        public void UploadBlob_DownloadBlobBinaryDataAsync_DeleteBlobAsyncTest()
        {
            // Arrange
            var blobStream = AzBlobUnitTestHelper.SampleTextStream;
            bool overwrite = false;
            CancellationToken cancellationToken = default;
            string blobContainerName = AzBlobUnitTestHelper.GetRandomBlobContainerNameFromDefault();
            string blobName = AzBlobUnitTestHelper.GetRandomBlobNameFromDefault();

            var snapshotsOption = DeleteSnapshotsOption.None;

            // Act
            var _uploadToBlobResponseAct = AzBlobUnitTestHelper.UploadBlob(blobStream, overwrite, cancellationToken,
                blobContainerName, blobName);
            var _downloadBlobBinaryDataAsyncResponseAct = AzBlobUnitTestHelper.DownloadBlobBinaryDataAsync(default, cancellationToken,
                blobContainerName, blobName).WaitAndUnwrapException();
            var _deleteBlobDeleteBlobContainerAsyncResponseAct = AzBlobUnitTestHelper.DeleteBlobDeleteBlobContainerAsync(snapshotsOption,
                default, cancellationToken, blobContainerName, blobName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_uploadToBlobResponseAct);
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_downloadBlobBinaryDataAsyncResponseAct);
            UnitTestHelper.AssertExpectedSuccessfulResponses(_deleteBlobDeleteBlobContainerAsyncResponseAct);
        }

        #endregion
    }
}
