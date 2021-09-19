using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using CoreTools.Extensions;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzCosmosDBRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    [Collection(nameof(DisableParallelizationCollection))]
    public class Sample6_DeleteEntity
    {
        [Fact, TestPriority(600)]
        public void DeleteEntityTest()
        {
            // Arrange
            var entity = AzCosmosUnitTestHelper.CreateSomeEntity();
            var _addEntityAsyncResponseAct = AzCosmosUnitTestHelper.AddEntity(entity);
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_addEntityAsyncResponseAct);

            // Act
            var _deleteEntityResponseAct = AzCosmosUnitTestHelper.DeleteEntity(entity);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_deleteEntityResponseAct);
        }

        [Fact, TestPriority(600)]
        public void DeleteEntityTest2()
        {
            // Arrange
            var entity = AzCosmosUnitTestHelper.CreateSomeEntity();
            var _addEntityAsyncResponseAct = AzCosmosUnitTestHelper.AddEntity(entity);
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_addEntityAsyncResponseAct);

            // Act
            var _deleteEntityResponseAct = AzCosmosUnitTestHelper.DeleteEntity2(entity);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_deleteEntityResponseAct);
        }

        #region Async

        [Fact, TestPriority(600)]
        public void DeleteEntityAsyncTest()
        {
            // Arrange
            var entity = AzCosmosUnitTestHelper.CreateSomeEntity();
            var _addEntityAsyncResponseAct = AzCosmosUnitTestHelper.AddEntityAsync(entity).WaitAndUnwrapException();
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_addEntityAsyncResponseAct);

            // Act
            var _deleteEntityResponseAct = AzCosmosUnitTestHelper.DeleteEntityAsync(entity).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_deleteEntityResponseAct);
        }

        [Fact, TestPriority(600)]
        public void DeleteEntityAsyncTest2()
        {
            // Arrange
            var entity = AzCosmosUnitTestHelper.CreateSomeEntity();
            var _addEntityAsyncResponseAct = AzCosmosUnitTestHelper.AddEntityAsync(entity).WaitAndUnwrapException();
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_addEntityAsyncResponseAct);

            // Act
            var _deleteEntityResponseAct = AzCosmosUnitTestHelper.DeleteEntityAsync2(entity).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_deleteEntityResponseAct);
        }
        #endregion
    }
}
