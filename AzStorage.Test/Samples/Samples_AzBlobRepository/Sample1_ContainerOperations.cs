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

namespace AzStorage.Test.Samples.Samples_AzBlobRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample1_ContainerOperations
    {
        #region Container operations sync

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

        [Fact, TestPriority(102)]
        public void FailedCreateBlobContainerTest()
        {
            // Arrange
            string blobContainerName1 = string.Empty;
            string blobContainerName2 = default;

            // Act
            // Assert
            Assert.Throws<NullReferenceException>(() => AzBlobUnitTestHelper
                .CreateBlobContainer(blobContainerName1));
            Assert.Throws<NullReferenceException>(() => AzBlobUnitTestHelper
                .CreateBlobContainer(blobContainerName2));
        }

        [Fact, TestPriority(104)]
        public void CreateBlobContainersDeleteBlobContainerTest2()
        {
            // Arrange
            var blobContainerNames = new List<string> { AzBlobUnitTestHelper.GetRandomBlobNameFromDefault() };
            blobContainerNames.Add(string.Empty);
            blobContainerNames.Add(default);

            // Act
            var _createBlobContainersAsyncResponseAct = AzBlobUnitTestHelper
                .CreateBlobContainers(blobContainerNames);
            var _deleteBlobContainerAsyncResponseAct = AzBlobUnitTestHelper
                .DeleteBlobContainer(blobContainerNames[0]);

            // Assert
            Assert.True(_createBlobContainersAsyncResponseAct[0].Succeeded);
            Assert.False(_createBlobContainersAsyncResponseAct[1].Succeeded);
            Assert.False(_createBlobContainersAsyncResponseAct[2].Succeeded);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_deleteBlobContainerAsyncResponseAct);
        }

        [Fact, TestPriority(106)]
        public void CreateBlobContainersCancellationTokenTest2()
        {
            // Arrange
            int namesAmount = 100;
            var blobContainerNames = AzBlobUnitTestHelper.GetRandomBlobNamesFromDefault(namesAmount);

            var cTokenSource = new CancellationTokenSource();
            CancellationToken token = cTokenSource.Token;

            // Act
            cTokenSource.CancelAfter(12000);
            var _createBlobContainersAsyncResponseAct = AzBlobUnitTestHelper
                .CreateBlobContainers(blobContainerNames, cancellationToken: token);

            // Assert
            UnitTestHelper.AssertExpectedCancelledGenResponses(_createBlobContainersAsyncResponseAct, namesAmount);
            AzBlobUnitTestHelper.AssertDeleteBlobContainerExpectedSuccessfulResponses(_createBlobContainersAsyncResponseAct);
        }

        [Fact, TestPriority(108)]
        public void CreateBlobContainersGetBlobContainersDeleteBlobContainersTest()
        {
            // Arrange
            int namesAmount = 10;
            var blobContainerNames = AzBlobUnitTestHelper.GetRandomBlobNamesFromDefault(namesAmount);

            // Act
            var _createBlobContainersResponseAct = AzBlobUnitTestHelper.CreateBlobContainers(blobContainerNames);
            var _getBlobContainersResponseAct = AzBlobUnitTestHelper.GetBlobContainers(
                prefix: AzBlobUnitTestHelper.GetDefaultBlobName);
            var _deleteBlobContainersResponseAct = AzBlobUnitTestHelper.DeleteBlobContainers(blobContainerNames);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponses(_createBlobContainersResponseAct);
            AzBlobUnitTestHelper.AssertExpectedSuccessfulGenResponse(_getBlobContainersResponseAct, blobContainerNames);
            UnitTestHelper.AssertExpectedSuccessfulResponses(_deleteBlobContainersResponseAct);
        }
        
        [Fact, TestPriority(110)]
        public void CreateBlobContainersGetBlobContainersWithTakeTest()
        {
            // Arrange
            int namesAmount = 10;
            var blobContainerNames = AzBlobUnitTestHelper.GetRandomBlobNamesFromDefault(namesAmount);
            int take = 3;

            // Act
            var _createBlobContainersResponseAct = AzBlobUnitTestHelper.CreateBlobContainers(blobContainerNames);
            var _getBlobContainersResponseAct = AzBlobUnitTestHelper.GetBlobContainers(
                prefix: AzBlobUnitTestHelper.GetDefaultBlobName, take: take);
            var _deleteBlobContainersResponseAct = AzBlobUnitTestHelper.DeleteBlobContainers(blobContainerNames);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponses(_createBlobContainersResponseAct);
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_getBlobContainersResponseAct);
            Assert.Equal(take, _getBlobContainersResponseAct.Value.Count);
            UnitTestHelper.AssertExpectedSuccessfulResponses(_deleteBlobContainersResponseAct);
        }

        #endregion

        #region Container operations async

        [Fact, TestPriority(150)]
        public void CreateBlobContainerAsyncDeleteBlobContainerAsyncTest()
        {
            // Arrange
            string blobContainerName = AzBlobUnitTestHelper.GetRandomBlobNameFromDefault();

            // Act
            var _createBlobContainerAsyncResponseAct = AzBlobUnitTestHelper
                .CreateBlobContainerAsync(blobContainerName).WaitAndUnwrapException();
            var _deleteBlobContainerAsyncResponseAct = AzBlobUnitTestHelper
                .DeleteBlobContainerAsync(blobContainerName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_createBlobContainerAsyncResponseAct);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_deleteBlobContainerAsyncResponseAct);
        }
        
        [Fact, TestPriority(152)]
        public void FailedCreateBlobContainerAsyncTest()
        {
            // Arrange
            string blobContainerName1 = string.Empty;
            string blobContainerName2 = default;

            // Act
            // Assert
            Assert.Throws<NullReferenceException>(() => AzBlobUnitTestHelper
                .CreateBlobContainerAsync(blobContainerName1).WaitAndUnwrapException());            
            Assert.Throws<NullReferenceException>(() => AzBlobUnitTestHelper
                .CreateBlobContainerAsync(blobContainerName2).WaitAndUnwrapException());
        }

        [Fact, TestPriority(154)]
        public void CreateBlobContainersAsyncDeleteBlobContainerAsyncTest2()
        {
            // Arrange
            var blobContainerNames = new List<string> { AzBlobUnitTestHelper.GetRandomBlobNameFromDefault() };
            blobContainerNames.Add(string.Empty);
            blobContainerNames.Add(default);

            // Act
            var _createBlobContainersAsyncResponseAct = AzBlobUnitTestHelper
                .CreateBlobContainersAsync(blobContainerNames).WaitAndUnwrapException();
            var _deleteBlobContainerAsyncResponseAct = AzBlobUnitTestHelper
                .DeleteBlobContainerAsync(blobContainerNames[0]).WaitAndUnwrapException();

            // Assert
            Assert.True(_createBlobContainersAsyncResponseAct[0].Succeeded);
            Assert.False(_createBlobContainersAsyncResponseAct[1].Succeeded);
            Assert.False(_createBlobContainersAsyncResponseAct[2].Succeeded);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_deleteBlobContainerAsyncResponseAct);
        }

        [Fact, TestPriority(156)]
        public void CreateBlobContainersAsyncCancellationTokenTest2()
        {
            // Arrange
            int namesAmount = 100;
            var blobContainerNames = AzBlobUnitTestHelper.GetRandomBlobNamesFromDefault(namesAmount);

            var cTokenSource = new CancellationTokenSource();
            CancellationToken token = cTokenSource.Token;

            // Act
            cTokenSource.CancelAfter(12000);
            var _createBlobContainersAsyncResponseAct = AzBlobUnitTestHelper
                .CreateBlobContainersAsync(blobContainerNames, cancellationToken: token).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedCancelledGenResponses(_createBlobContainersAsyncResponseAct, namesAmount);
            AzBlobUnitTestHelper.AssertDeleteBlobContainerExpectedSuccessfulResponses(_createBlobContainersAsyncResponseAct);
        }

        [Fact, TestPriority(158)]
        public void CreateBlobContainersAsyncGetBlobContainersAsyncDeleteBlobContainersAsyncTest()
        {
            // Arrange
            int namesAmount = 10;
            var blobContainerNames = AzBlobUnitTestHelper.GetRandomBlobNamesFromDefault(namesAmount);

            // Act
            var _createBlobContainersAsyncResponseAct = AzBlobUnitTestHelper
                .CreateBlobContainersAsync(blobContainerNames).WaitAndUnwrapException();
            var _getBlobContainersAsyncResponseAct = AzBlobUnitTestHelper
                .GetBlobContainersAsync(prefix: AzBlobUnitTestHelper.GetDefaultBlobName).WaitAndUnwrapException();
            var _deleteBlobContainersAsyncResponseAct = AzBlobUnitTestHelper
                .DeleteBlobContainersAsync(blobContainerNames).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponses(_createBlobContainersAsyncResponseAct);
            AzBlobUnitTestHelper.AssertExpectedSuccessfulGenResponse(_getBlobContainersAsyncResponseAct, blobContainerNames);
            UnitTestHelper.AssertExpectedSuccessfulResponses(_deleteBlobContainersAsyncResponseAct);
        }
        
        [Fact, TestPriority(158)]
        public void CreateBlobContainersAsyncGetAllBlobContainersAsyncWithTakeTest()
        {
            // Arrange
            int namesAmount = 10;
            var blobContainerNames = AzBlobUnitTestHelper.GetRandomBlobNamesFromDefault(namesAmount);
            int take = 9;

            // Act
            var _createBlobContainersAsyncResponseAct = AzBlobUnitTestHelper
                .CreateBlobContainersAsync(blobContainerNames).WaitAndUnwrapException();
            var _getBlobContainersAsyncResponseAct = AzBlobUnitTestHelper
                .GetAllBlobContainersAsync(take: take).WaitAndUnwrapException();
            var _deleteBlobContainersAsyncResponseAct = AzBlobUnitTestHelper
                .DeleteBlobContainersAsync(blobContainerNames).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponses(_createBlobContainersAsyncResponseAct);
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_getBlobContainersAsyncResponseAct);
            Assert.Equal(take, _getBlobContainersAsyncResponseAct.Value.Count);
            UnitTestHelper.AssertExpectedSuccessfulResponses(_deleteBlobContainersAsyncResponseAct);
        }

        [Fact, TestPriority(158)]
        public void CreateBlobContainersAsyncGetAllBlobContainersAsyncTest()
        {
            // Arrange
            int namesAmount = 10;
            var blobContainerNames = AzBlobUnitTestHelper.GetRandomBlobNamesFromDefault(namesAmount);

            // Act
            var _createBlobContainersAsyncResponseAct = AzBlobUnitTestHelper
                .CreateBlobContainersAsync(blobContainerNames).WaitAndUnwrapException();
            var _getBlobContainersAsyncResponseAct = AzBlobUnitTestHelper
                .GetAllBlobContainersAsync(BlobContainerTraits.None, BlobContainerStates.None,
                    default, CreateResourcePolicy.OnlyFirstTime).WaitAndUnwrapException();
            var _deleteBlobContainersAsyncResponseAct = AzBlobUnitTestHelper
                .DeleteBlobContainersAsync(blobContainerNames).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponses(_createBlobContainersAsyncResponseAct);
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_getBlobContainersAsyncResponseAct);
            UnitTestHelper.AssertExpectedSuccessfulResponses(_deleteBlobContainersAsyncResponseAct);
        }

        [Fact, TestPriority(160)]
        public void CreateBlobContainersAsyncGetBlobContainersAsyncWithTakeTest()
        {
            // Arrange
            int namesAmount = 10;
            var blobContainerNames = AzBlobUnitTestHelper.GetRandomBlobNamesFromDefault(namesAmount);
            int take = 3;

            // Act
            var _createBlobContainersAsyncResponseAct = AzBlobUnitTestHelper
                .CreateBlobContainersAsync(blobContainerNames).WaitAndUnwrapException();
            var _getBlobContainersAsyncResponseAct = AzBlobUnitTestHelper
                .GetBlobContainersAsync(prefix: AzBlobUnitTestHelper.GetDefaultBlobName, take: take).WaitAndUnwrapException();
            var _deleteBlobContainersAsyncResponseAct = AzBlobUnitTestHelper
                .DeleteBlobContainersAsync(blobContainerNames).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponses(_createBlobContainersAsyncResponseAct);
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_getBlobContainersAsyncResponseAct);
            Assert.Equal(take, _getBlobContainersAsyncResponseAct.Value.Count);
            UnitTestHelper.AssertExpectedSuccessfulResponses(_deleteBlobContainersAsyncResponseAct);
        }

        #endregion
    }
}
