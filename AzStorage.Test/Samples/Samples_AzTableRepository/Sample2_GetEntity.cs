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
        [Fact, TestPriority(90)]
        public void GetEntityTest()
        {
            // Arrange
            var entity = AzTableUnitTestHelper.CreateSomeEntity();
            var _addEntityResponseArr = AzTableUnitTestHelper.AddEntity(entity);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_addEntityResponseArr);

            // Act
            var _getEntityResponseAct = AzTableUnitTestHelper.GetEntity<TableEntity>(entity.PartitionKey, entity.RowKey);

            // Assert
            AzTableUnitTestHelper.AssertExpectedSuccessfulGenResponse(entity, _getEntityResponseAct);
        }

        [Fact, TestPriority(92)]
        public void GetNotExistingEntityTest()
        {
            // Arrange

            // Act
            var _getEntityResponseAct = AzTableUnitTestHelper.GetEntity<TableEntity>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

            // Assert
            UnitTestHelper.AssertExpectedFailedGenResponse(_getEntityResponseAct, ConstProvider.The_specified_resource_does_not_exist);
        }

        #region Async

        [Fact, TestPriority(91)]
        public void GetEntityAsyncTest()
        {
            // Arrange
            var entity = AzTableUnitTestHelper.CreateSomeEntity();
            var _addEntityResponseArr = AzTableUnitTestHelper.AddEntity(entity);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_addEntityResponseArr);

            // Act
            var _getEntityResponseAct = Helper.RunAsSync(entity.PartitionKey, entity.RowKey, 
                AzTableUnitTestHelper.GetEntityAsync<TableEntity>);

            // Assert
            AzTableUnitTestHelper.AssertExpectedSuccessfulGenResponse(entity, _getEntityResponseAct);
        }

        #endregion
    }
}
