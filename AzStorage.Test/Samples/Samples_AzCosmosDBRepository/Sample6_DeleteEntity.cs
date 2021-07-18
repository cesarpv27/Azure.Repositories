using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using CoreTools.Extensions;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzCosmosDBRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample6_DeleteEntity
    {
        [Fact, TestPriority(100)]
        public void DeleteEntityAsyncTest()
        {
            // Arrange
            var entity = AzCosmosUnitTestHelper.CreateSomeEntity();
            var _addEntityAsyncResponseAct = AzCosmosUnitTestHelper.AddEntityAsync(entity).WaitAndUnwrapException();
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_addEntityAsyncResponseAct);

            // Act
            var _deleteEntityResponseAct = AzCosmosUnitTestHelper.DeleteEntityAsync(entity).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_deleteEntityResponseAct);
        }
        
        [Fact, TestPriority(100)]
        public void DeleteEntityAsyncTest2()
        {
            // Arrange
            var entity = AzCosmosUnitTestHelper.CreateSomeEntity();
            var _addEntityAsyncResponseAct = AzCosmosUnitTestHelper.AddEntityAsync(entity).WaitAndUnwrapException();
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_addEntityAsyncResponseAct);

            // Act
            var _deleteEntityResponseAct = AzCosmosUnitTestHelper.DeleteEntityAsync2(entity).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_deleteEntityResponseAct);
        }
    }
}
