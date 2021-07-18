using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using CoreTools.Extensions;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzCosmosDBRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample1_AddEntity
    {
        [Fact, TestPriority(100)]
        public void AddEntityAsyncTest()
        {
            // Arrange
            var entity = AzCosmosUnitTestHelper.CreateSomeEntity();

            // Act
            var _addEntityAsyncResponseAct = AzCosmosUnitTestHelper.AddEntityAsync(entity).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_addEntityAsyncResponseAct);
        }
        
        [Fact, TestPriority(100)]
        public void AddEntityAsyncTest2()
        {
            // Arrange
            var entity = AzCosmosUnitTestHelper.CreateSomeEntity();

            // Act
            var _addEntityAsyncResponseAct = AzCosmosUnitTestHelper.AddEntityAsync2(entity).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_addEntityAsyncResponseAct);
        }

    }
}
