using System;
using Xunit;
using AzStorage.Test.Utilities;
using Azure.Data.Tables;
using AzStorage.Test.Helpers;

namespace AzStorage.Test.Samples.Samples_AzTableRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample0_Miscellaneous
    {
        [Fact, TestPriority(100)]
        public void CreateAzTableRepositoryTest1()
        {
            Assert.Throws<ArgumentNullException>(() => new Repositories.AzTableRepository(connectionString: string.Empty));
        }

        [Fact, TestPriority(100)]
        public void GetTableNameTest1()
        {
            // Arrange
            string partitionKey = Guid.NewGuid().ToString();
            string rowKey = Guid.NewGuid().ToString();

            var sampleEntity = new SampleTableEntity
            {
                PartitionKey = partitionKey,
                RowKey = rowKey
            };

            var tableName = typeof(SampleTableEntity).Name;

            // Arr
            var _addEntityResponseAct = AzTableUnitTestHelper.AddEntity(sampleEntity);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_addEntityResponseAct);

            var _getEntityResponse = AzTableUnitTestHelper.GetEntity<TableEntity>(partitionKey, rowKey, tableName: tableName);
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_getEntityResponse);
            var recoveredEntity = _getEntityResponse.Value;

            Assert.Equal(sampleEntity.PartitionKey, recoveredEntity.PartitionKey);
            Assert.Equal(sampleEntity.RowKey, recoveredEntity.RowKey);
        }

        [Fact, TestPriority(100)]
        public void GetTableNameTest2()
        {
            // Arrange
            string partitionKey = Guid.NewGuid().ToString();
            string rowKey = Guid.NewGuid().ToString();

            var sampleEntity = new SampleTableEntity
            {
                PartitionKey = partitionKey,
                RowKey = rowKey
            };

            // Arr
            var _addEntityResponseAct = AzTableUnitTestHelper.AddEntity(sampleEntity);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_addEntityResponseAct);

            var _getEntityResponse = AzTableUnitTestHelper.GetEntity<SampleTableEntity>(partitionKey, rowKey);
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_getEntityResponse);

            var recoveredEntity = _getEntityResponse.Value;

            Assert.Equal(sampleEntity.PartitionKey, recoveredEntity.PartitionKey);
            Assert.Equal(sampleEntity.RowKey, recoveredEntity.RowKey);
        }

        [Fact, TestPriority(100)]
        public void GetTableNameTest3()
        {
            // Arrange
            var entity = AzTableUnitTestHelper.CreateSomeEntity();

            var tableName = typeof(SampleTableEntity).Name;

            // Arr
            var _addEntityResponseAct = AzTableUnitTestHelper.AddEntity(entity, tableName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_addEntityResponseAct);

            var _getEntityResponse = AzTableUnitTestHelper.GetEntity<TableEntity>(entity.PartitionKey, entity.RowKey, tableName: tableName);
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_getEntityResponse);

            var recoveredEntity = _getEntityResponse.Value;

            Assert.Equal(entity.PartitionKey, recoveredEntity.PartitionKey);
            Assert.Equal(entity.RowKey, recoveredEntity.RowKey);

            _getEntityResponse = AzTableUnitTestHelper.GetEntity<TableEntity>(entity.PartitionKey, entity.RowKey);
            UnitTestHelper.AssertExpectedFailedGenResponse(_getEntityResponse, ConstProvider.The_specified_resource_does_not_exist);
        }

        [Fact, TestPriority(100)]
        public void CreateTableTest()
        {
            // Arrange
            var entity = AzTableUnitTestHelper.CreateSomeEntity();

            var tableName = $"{typeof(SampleTableEntity).Name}{AzTableUnitTestHelper.GetOverOneHundredRandomValue()}";

            // Arr
            var _addEntityResponseAct = AzTableUnitTestHelper.AddEntity(entity, tableName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_addEntityResponseAct);

            var _getEntityResponse = AzTableUnitTestHelper.GetEntity<TableEntity>(entity.PartitionKey, entity.RowKey, tableName: tableName);
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_getEntityResponse);

            _getEntityResponse = AzTableUnitTestHelper.GetEntity<TableEntity>(entity.PartitionKey, entity.RowKey);
            UnitTestHelper.AssertExpectedFailedGenResponse(_getEntityResponse, ConstProvider.The_specified_resource_does_not_exist);
        }
    }
}
