using System;
using Xunit;
using AzStorage.Test.Utilities;
using AzStorage.Test.Helpers;
using Azure.Data.Tables;
using CoreTools.Helpers;
using System.Threading.Tasks;
using AzCoreTools.Core;

namespace AzStorage.Test.Samples
{
    public class Sample2_GetEntity
    {
        [Fact]
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

        [Fact]
        public void GetNotExistingEntityTest()
        {
            // Arrange

            // Act
            var _getEntityResponseAct = UnitTestHelper.GetEntity<TableEntity>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

            // Assert
            UnitTestHelper.AssertExpectedFailedGenResponse(_getEntityResponseAct, ConstProvider.The_specified_resource_does_not_exist);
        }

        #region Async

        [Fact]
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
