using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Xunit;
using Azure.Storage.Blobs.Models;

namespace AzStorage.Test.Samples.Samples_AzBlobRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample2_UploadBlob_DeleteBlob
    {
        [Fact, TestPriority(210)]
        public void UploadBlob_DeleteBlobTest()
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
            var _deleteBlobDeleteBlobContainerResponseAct = AzBlobUnitTestHelper.DeleteBlobDeleteBlobContainer(snapshotsOption,
                default, cancellationToken, blobContainerName, blobName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_uploadToBlobResponseAct);
            UnitTestHelper.AssertExpectedSuccessfulResponses(_deleteBlobDeleteBlobContainerResponseAct);
        }

    }
}
