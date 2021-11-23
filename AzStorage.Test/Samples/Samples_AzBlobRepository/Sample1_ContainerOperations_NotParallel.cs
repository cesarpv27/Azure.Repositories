using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using CoreTools.Extensions;
using System.Threading;
using AzCoreTools.Core;
using Azure.Storage.Blobs.Models;
using AzStorage.Core.Blobs;

namespace AzStorage.Test.Samples.Samples_AzBlobRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    [Collection(nameof(DisableParallelizationCollection))]
    public class Sample1_ContainerOperations_NotParallel
    {
        #region Container operations sync

        [Fact, TestPriority(10)]
        public void DeleteAllBlobContainersTest()
        {
            // Arrange
            int namesAmount = 10;
            var blobContainerNames = AzBlobUnitTestHelper.GetRandomBlobContainerNamesFromDefault(namesAmount);
            var _createBlobContainersResponseAct = AzBlobUnitTestHelper
                .CreateBlobContainers(blobContainerNames);
            UnitTestHelper.AssertExpectedSuccessfulGenResponses(_createBlobContainersResponseAct);

            // Act
            var _deleteAllBlobContainersResponseAct = AzBlobUnitTestHelper.DeleteAllBlobContainers();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponses(_deleteAllBlobContainersResponseAct);

            var _getBlobContainersResponseAct = AzBlobUnitTestHelper.GetAllBlobContainers();
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_getBlobContainersResponseAct);
            Assert.Empty(_getBlobContainersResponseAct.Value);
        }

        #endregion

        #region Container operations async

        [Fact, TestPriority(10)]
        public void DeleteAllBlobContainersAsyncTest()
        {
            // Arrange
            int namesAmount = 10;
            var blobContainerNames = AzBlobUnitTestHelper.GetRandomBlobContainerNamesFromDefault(namesAmount);
            var _createBlobContainersAsyncResponseAct = AzBlobUnitTestHelper
                .CreateBlobContainersAsync(blobContainerNames).WaitAndUnwrapException();
            UnitTestHelper.AssertExpectedSuccessfulGenResponses(_createBlobContainersAsyncResponseAct);

            // Act
            var _deleteAllBlobContainersAsyncResponseAct = AzBlobUnitTestHelper
                .DeleteAllBlobContainersAsync().WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponses(_deleteAllBlobContainersAsyncResponseAct);

            var _getBlobContainersAsyncResponseAct = AzBlobUnitTestHelper
                .GetAllBlobContainersAsync().WaitAndUnwrapException();
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_getBlobContainersAsyncResponseAct);
            Assert.Empty(_getBlobContainersAsyncResponseAct.Value);
        }

        #endregion
    }
}
