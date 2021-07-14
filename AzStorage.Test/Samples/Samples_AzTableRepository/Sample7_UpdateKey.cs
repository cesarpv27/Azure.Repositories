using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using Azure.Data.Tables;
using CoreTools.Extensions;
using System;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzTableRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample7_UpdateKey
    {
        [Fact, TestPriority(100)]
        public void UpdateKeysTest1()
        {
            // Arrange
            string partitionKey = Guid.NewGuid().ToString();
            string rowKey = Guid.NewGuid().ToString();
            var entity = AzTableUnitTestHelper.CreateSomeEntity(partitionKey, rowKey);
            AzTableUnitTestHelper.AssertExpectedSuccessfulResponse(AzTableUnitTestHelper.AddEntity(entity));

            string newPartitionKey = Guid.NewGuid().ToString();
            string newRowKey = Guid.NewGuid().ToString();

            // Act
            var _updateKeysResponseAct = AzTableUnitTestHelper.UpdateKeys<TableEntity>(partitionKey, rowKey, newPartitionKey, newRowKey);

            // Assert
            AzTableUnitTestHelper.AssertExpectedSuccessfulResponse(_updateKeysResponseAct);

            var _getEntityResponse = AzTableUnitTestHelper.GetEntity<TableEntity>(newPartitionKey, newRowKey);
            AzTableUnitTestHelper.AssertExpectedSuccessfulGenResponse(_getEntityResponse);

            _getEntityResponse = AzTableUnitTestHelper.GetEntity<TableEntity>(partitionKey, rowKey);
            AzTableUnitTestHelper.AssertExpectedFailedGenResponse(_getEntityResponse, ConstProvider.The_specified_resource_does_not_exist);
        }

        [Fact, TestPriority(100)]
        public void UpdateKeysTest2()
        {
            // Arrange
            string partitionKey = Guid.NewGuid().ToString();
            string rowKey = Guid.NewGuid().ToString();
            var entity = AzTableUnitTestHelper.CreateSomeEntity(partitionKey, rowKey);
            AzTableUnitTestHelper.AssertExpectedSuccessfulResponse(AzTableUnitTestHelper.AddEntity(entity));

            string newPartitionKey = partitionKey;
            string newRowKey = Guid.NewGuid().ToString();

            // Act
            var _updateKeysResponseAct = AzTableUnitTestHelper.UpdateKeys<TableEntity>(partitionKey, rowKey, newPartitionKey, newRowKey);

            // Assert
            AzTableUnitTestHelper.AssertExpectedSuccessfulResponse(_updateKeysResponseAct);

            var _getEntityResponse = AzTableUnitTestHelper.GetEntity<TableEntity>(newPartitionKey, newRowKey);
            AzTableUnitTestHelper.AssertExpectedSuccessfulGenResponse(_getEntityResponse);

            _getEntityResponse = AzTableUnitTestHelper.GetEntity<TableEntity>(partitionKey, rowKey);
            AzTableUnitTestHelper.AssertExpectedFailedGenResponse(_getEntityResponse, ConstProvider.The_specified_resource_does_not_exist);
        }

        [Fact, TestPriority(100)]
        public void UpdateKeysTest3()
        {
            string nonEmptyKey = "SomeKey";
            Assert.Throws<ArgumentNullException>(() => AzTableUnitTestHelper.UpdateKeys<TableEntity>(string.Empty, nonEmptyKey, nonEmptyKey, nonEmptyKey));
            Assert.Throws<ArgumentNullException>(() => AzTableUnitTestHelper.UpdateKeys<TableEntity>(nonEmptyKey, string.Empty, nonEmptyKey, nonEmptyKey));
            Assert.Throws<ArgumentNullException>(() => AzTableUnitTestHelper.UpdateKeys<TableEntity>(nonEmptyKey, nonEmptyKey, string.Empty, nonEmptyKey));
            Assert.Throws<ArgumentNullException>(() => AzTableUnitTestHelper.UpdateKeys<TableEntity>(nonEmptyKey, nonEmptyKey, nonEmptyKey, string.Empty));

            var argException = Assert.Throws<ArgumentException>(() => AzTableUnitTestHelper.UpdateKeys<TableEntity>(nonEmptyKey, nonEmptyKey, nonEmptyKey, nonEmptyKey));
            Assert.Equal(Core.Texting.ErrorTextProvider.Current_keys_same_as_new_keys, argException.GetDepthMessages());
        }

        [Fact, TestPriority(100)]
        public void UpdatePartitionKeyTest()
        {
            // Arrange
            string partitionKey = Guid.NewGuid().ToString();
            string rowKey = Guid.NewGuid().ToString();
            var entity = AzTableUnitTestHelper.CreateSomeEntity(partitionKey, rowKey);

            AzTableUnitTestHelper.AssertExpectedSuccessfulResponse(AzTableUnitTestHelper.AddEntity(entity));

            string newPartitionKey = Guid.NewGuid().ToString();

            // Act
            var _updatePartitionKeyResponseAct = AzTableUnitTestHelper.UpdatePartitionKey<TableEntity>(partitionKey, rowKey, newPartitionKey);

            // Assert
            AzTableUnitTestHelper.AssertExpectedSuccessfulResponse(_updatePartitionKeyResponseAct);

            var _getEntityResponse = AzTableUnitTestHelper.GetEntity<TableEntity>(newPartitionKey, rowKey);
            AzTableUnitTestHelper.AssertExpectedSuccessfulGenResponse(_getEntityResponse);

            _getEntityResponse = AzTableUnitTestHelper.GetEntity<TableEntity>(partitionKey, rowKey);
            AzTableUnitTestHelper.AssertExpectedFailedGenResponse(_getEntityResponse, ConstProvider.The_specified_resource_does_not_exist);
        }

        [Fact, TestPriority(100)]
        public void UpdateRowKeyTest()
        {
            // Arrange
            string partitionKey = Guid.NewGuid().ToString();
            string rowKey = Guid.NewGuid().ToString();
            var entity = AzTableUnitTestHelper.CreateSomeEntity(partitionKey, rowKey);

            AzTableUnitTestHelper.AssertExpectedSuccessfulResponse(AzTableUnitTestHelper.AddEntity(entity));

            string newRowKey = Guid.NewGuid().ToString();

            // Act
            var _updateRowKeyResponseAct = AzTableUnitTestHelper.UpdateRowKey<TableEntity>(partitionKey, rowKey, newRowKey);

            // Assert
            AzTableUnitTestHelper.AssertExpectedSuccessfulResponse(_updateRowKeyResponseAct);

            var _getEntityResponse = AzTableUnitTestHelper.GetEntity<TableEntity>(partitionKey, newRowKey);
            AzTableUnitTestHelper.AssertExpectedSuccessfulGenResponse(_getEntityResponse);

            _getEntityResponse = AzTableUnitTestHelper.GetEntity<TableEntity>(partitionKey, rowKey);
            AzTableUnitTestHelper.AssertExpectedFailedGenResponse(_getEntityResponse, ConstProvider.The_specified_resource_does_not_exist);
        }
    }
}
