using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using Azure.Data.Tables;
using CoreTools.Helpers;
using System;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzTableRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample2_GetEntity
    {
        [Fact, TestPriority(100)]
        public void GetEntityTest()
        {
            // Arrange
            var entity = UnitTestHelper.CreateSomeEntity();
            var _addEntityResponseArr = UnitTestHelper.AddEntity(entity);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_addEntityResponseArr);

            // Act
            var _getEntityResponseAct = UnitTestHelper.GetEntity<TableEntity>(entity.PartitionKey, entity.RowKey);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(entity, _getEntityResponseAct);
        }

        [Fact, TestPriority(100)]
        public void GetNotExistingEntityTest()
        {
            // Arrange

            // Act
            var _getEntityResponseAct = UnitTestHelper.GetEntity<TableEntity>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

            // Assert
            UnitTestHelper.AssertExpectedFailedGenResponse(_getEntityResponseAct, ConstProvider.The_specified_resource_does_not_exist);
        }

        #region Async

        [Fact, TestPriority(100)]
        public void GetEntityAsyncTest()
        {
            // Arrange
            var entity = UnitTestHelper.CreateSomeEntity();
            var _addEntityResponseArr = UnitTestHelper.AddEntity(entity);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_addEntityResponseArr);

            // Act
            var _getEntityResponseAct = Helper.RunAsSync(entity.PartitionKey, entity.RowKey, 
                UnitTestHelper.GetEntityAsync<TableEntity>);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(entity, _getEntityResponseAct);
        }

        #endregion
    }
}
