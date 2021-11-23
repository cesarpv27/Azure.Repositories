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
    public class Sample2_UploadToBlob
    {
        [Fact, TestPriority(100)]
        public void UploadToBlobTest()
        {
            // Arrange
            var blobStream = AzBlobUnitTestHelper.SampleTextStream;
            bool overwrite = false;
            CancellationToken cancellationToken = default;
            string blobContainerNameName = AzBlobUnitTestHelper.GetRandomBlobContainerNameFromDefault();
            string blobName = AzBlobUnitTestHelper.GetRandomBlobNameFromDefault();

            var snapshotsOption = DeleteSnapshotsOption.None;

            // Act
            var _uploadToBlobResponseAct = AzBlobUnitTestHelper.UploadToBlob(blobStream, overwrite, cancellationToken,
                blobContainerNameName, blobName);
            var _deleteBlobResponseAct = AzBlobUnitTestHelper.DeleteBlob(snapshotsOption, default, cancellationToken,
                default, blobName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_uploadToBlobResponseAct);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_deleteBlobResponseAct);
        }

    }
}
