using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using System;
using System.Collections.Generic;
using AzCoreTools.Core;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzBlobRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample0_Miscellaneous
    {
        [Fact, TestPriority(10)]
        public void Repeat__CreateAzBlobRepository_DeleteBlobContainerTest()
        {
            // Arrange
            string blobContainerName = AzBlobUnitTestHelper.GetRandomBlobContainerNameFromDefault();
            string blobName = AzBlobUnitTestHelper.GetRandomBlobNameFromDefault();

            // Act
            var azBlobRepository_1 = AzBlobUnitTestHelper.CreateAzBlobRepository(
                blobContainerName, blobName, CreateResourcePolicy.Always);
            var azBlobRepository_2 = AzBlobUnitTestHelper.CreateAzBlobRepository(
                blobContainerName, blobName, CreateResourcePolicy.Always);

            var _deleteBlobContainerResponseAct = AzBlobUnitTestHelper.DeleteBlobContainer(blobContainerName);

            // Assert
            Assert.NotNull(azBlobRepository_1);
            Assert.NotNull(azBlobRepository_2);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_deleteBlobContainerResponseAct);
        }

    }
}
