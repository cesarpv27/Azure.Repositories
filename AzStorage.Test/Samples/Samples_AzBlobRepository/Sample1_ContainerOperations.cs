using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;

namespace AzStorage.Test.Samples.Samples_AzBlobRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample1_ContainerOperations
    {
        [Fact, TestPriority(100)]
        public void CreateBlobContainerDeleteBlobContainerTest()
        {
            // Arrange
            string blobContainerName = AzBlobUnitTestHelper.GetRandomBlobNameFromDefault();

            // Act
            var _createBlobContainerResponseAct = AzBlobUnitTestHelper.CreateBlobContainer(blobContainerName);
            var _deleteBlobContainerResponseAct = AzBlobUnitTestHelper.DeleteBlobContainer(blobContainerName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_createBlobContainerResponseAct);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_deleteBlobContainerResponseAct);
        }

    }
}
