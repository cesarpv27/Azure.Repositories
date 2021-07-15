using AzStorage.Test.Utilities;
using AzStorage.Test.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using CoreTools.Extensions;

namespace AzStorage.Test.Samples.Samples_AzCosmosDBRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample2_GetEntity
    {
        [Fact, TestPriority(100)]
        public void GetEntityAsyncTest()
        {
            // Arrange
            var entity = AzCosmosUnitTestHelper.CreateSomeEntity();
            var _addEntityResponseArr = AzCosmosUnitTestHelper.AddEntityAsync(entity).WaitAndUnwrapException();
            AzCosmosUnitTestHelper.AssertExpectedSuccessfulResponse(_addEntityResponseArr);

            // Act
            var _getEntityResponseAct = AzCosmosUnitTestHelper.GetEntityByIdAsync<CustomCosmosEntity>(
                entity.PartitionKey, entity.Id).WaitAndUnwrapException();

            // Assert
            AzCosmosUnitTestHelper.AssertExpectedSuccessfulGenResponse(entity, _getEntityResponseAct);
        }

    }
}
