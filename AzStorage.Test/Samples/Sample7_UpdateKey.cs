using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using AzStorage.Test.Utilities;
using Azure.Data.Tables;
using AzStorage.Test.Helpers;
using System.Linq;
using CoreTools.Extensions;

namespace AzStorage.Test.Samples
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
            var entity = UnitTestHelper.CreateSomeEntity(partitionKey, rowKey);
            UnitTestHelper.AssertExpectedSuccessfulResponse(UnitTestHelper.AddEntity(entity));

            string newPartitionKey = Guid.NewGuid().ToString();
            string newRowKey = Guid.NewGuid().ToString();

            // Act
            var _updateKeysResponseAct = UnitTestHelper.UpdateKeys<TableEntity>(partitionKey, rowKey, newPartitionKey, newRowKey);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_updateKeysResponseAct);

            var _getEntityResponse = UnitTestHelper.GetEntity<TableEntity>(newPartitionKey, newRowKey);
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_getEntityResponse);

            _getEntityResponse = UnitTestHelper.GetEntity<TableEntity>(partitionKey, rowKey);
            UnitTestHelper.AssertExpectedFailedGenResponse(_getEntityResponse, ConstProvider.The_specified_resource_does_not_exist);
        }

        [Fact, TestPriority(100)]
        public void UpdateKeysTest2()
        {
            // Arrange
            string partitionKey = Guid.NewGuid().ToString();
            string rowKey = Guid.NewGuid().ToString();
            var entity = UnitTestHelper.CreateSomeEntity(partitionKey, rowKey);
            UnitTestHelper.AssertExpectedSuccessfulResponse(UnitTestHelper.AddEntity(entity));

            string newPartitionKey = partitionKey;
            string newRowKey = Guid.NewGuid().ToString();

            // Act
            var _updateKeysResponseAct = UnitTestHelper.UpdateKeys<TableEntity>(partitionKey, rowKey, newPartitionKey, newRowKey);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_updateKeysResponseAct);

            var _getEntityResponse = UnitTestHelper.GetEntity<TableEntity>(newPartitionKey, newRowKey);
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_getEntityResponse);

            _getEntityResponse = UnitTestHelper.GetEntity<TableEntity>(partitionKey, rowKey);
            UnitTestHelper.AssertExpectedFailedGenResponse(_getEntityResponse, ConstProvider.The_specified_resource_does_not_exist);
        }

        [Fact, TestPriority(100)]
        public void UpdateKeysTest3()
        {
            string nonEmptyKey = "SomeKey";
            Assert.Throws<ArgumentNullException>(() => UnitTestHelper.UpdateKeys<TableEntity>(string.Empty, nonEmptyKey, nonEmptyKey, nonEmptyKey));
            Assert.Throws<ArgumentNullException>(() => UnitTestHelper.UpdateKeys<TableEntity>(nonEmptyKey, string.Empty, nonEmptyKey, nonEmptyKey));
            Assert.Throws<ArgumentNullException>(() => UnitTestHelper.UpdateKeys<TableEntity>(nonEmptyKey, nonEmptyKey, string.Empty, nonEmptyKey));
            Assert.Throws<ArgumentNullException>(() => UnitTestHelper.UpdateKeys<TableEntity>(nonEmptyKey, nonEmptyKey, nonEmptyKey, string.Empty));

            var argException = Assert.Throws<ArgumentException>(() => UnitTestHelper.UpdateKeys<TableEntity>(nonEmptyKey, nonEmptyKey, nonEmptyKey, nonEmptyKey));
            Assert.Equal(Core.Texting.ErrorTextProvider.Current_keys_same_as_new_keys, argException.GetDepthMessages());
        }

        [Fact, TestPriority(100)]
        public void UpdatePartitionKeyTest()
        {
            // Arrange
            string partitionKey = Guid.NewGuid().ToString();
            string rowKey = Guid.NewGuid().ToString();
            var entity = UnitTestHelper.CreateSomeEntity(partitionKey, rowKey);

            UnitTestHelper.AssertExpectedSuccessfulResponse(UnitTestHelper.AddEntity(entity));

            string newPartitionKey = Guid.NewGuid().ToString();

            // Act
            var _updatePartitionKeyResponseAct = UnitTestHelper.UpdatePartitionKey<TableEntity>(partitionKey, rowKey, newPartitionKey);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_updatePartitionKeyResponseAct);

            var _getEntityResponse = UnitTestHelper.GetEntity<TableEntity>(newPartitionKey, rowKey);
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_getEntityResponse);

            _getEntityResponse = UnitTestHelper.GetEntity<TableEntity>(partitionKey, rowKey);
            UnitTestHelper.AssertExpectedFailedGenResponse(_getEntityResponse, ConstProvider.The_specified_resource_does_not_exist);
        }

        [Fact, TestPriority(100)]
        public void UpdateRowKeyTest()
        {
            // Arrange
            string partitionKey = Guid.NewGuid().ToString();
            string rowKey = Guid.NewGuid().ToString();
            var entity = UnitTestHelper.CreateSomeEntity(partitionKey, rowKey);

            UnitTestHelper.AssertExpectedSuccessfulResponse(UnitTestHelper.AddEntity(entity));

            string newRowKey = Guid.NewGuid().ToString();

            // Act
            var _updateRowKeyResponseAct = UnitTestHelper.UpdateRowKey<TableEntity>(partitionKey, rowKey, newRowKey);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_updateRowKeyResponseAct);

            var _getEntityResponse = UnitTestHelper.GetEntity<TableEntity>(partitionKey, newRowKey);
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_getEntityResponse);

            _getEntityResponse = UnitTestHelper.GetEntity<TableEntity>(partitionKey, rowKey);
            UnitTestHelper.AssertExpectedFailedGenResponse(_getEntityResponse, ConstProvider.The_specified_resource_does_not_exist);
        }
    }
}
