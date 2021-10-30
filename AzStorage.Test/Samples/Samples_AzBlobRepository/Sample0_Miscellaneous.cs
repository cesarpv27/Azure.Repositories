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
        //[Fact, TestPriority(103)]
        //public void UpsertBlobContainerDeleteBlobContainerTest()
        //{
        //    // Arrange
        //    string blobContainerName = AzBlobUnitTestHelper.GetRandomBlobNameFromDefault();

        //    // Act
        //    var _upsertBlobContainerResponseAct = AzBlobUnitTestHelper.UpsertBlobContainer(blobContainerName);
        //    var _deleteBlobContainerResponseAct = AzBlobUnitTestHelper.DeleteBlobContainer(blobContainerName);

        //    // Assert
        //    UnitTestHelper.AssertExpectedSuccessfulGenResponse(_upsertBlobContainerResponseAct);
        //    UnitTestHelper.AssertExpectedSuccessfulResponse(_deleteBlobContainerResponseAct);
        //}

    }
}
