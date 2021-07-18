using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using CoreTools.Extensions;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzCosmosDBRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample2_GetEntity
    {
        [Fact, TestPriority(90)]
        public void GetEntityAsyncTest()
        {
            // Arrange
            var entity = AzCosmosUnitTestHelper.CreateSomeEntity();
            var _addEntityResponseArr = AzCosmosUnitTestHelper.AddEntityAsync(entity).WaitAndUnwrapException();
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_addEntityResponseArr);

            // Act
            var _getEntityResponseAct = AzCosmosUnitTestHelper.GetEntityByIdAsync<CustomCosmosEntity>(
                entity.PartitionKey, entity.Id).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_getEntityResponseAct);
        }
    }
}
