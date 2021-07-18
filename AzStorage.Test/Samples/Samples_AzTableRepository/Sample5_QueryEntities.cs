using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzTableRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample5_QueryEntities
    {
        [Fact, TestPriority(200)]
        public void QueryAllTest()
        {
            // Arrange
            AzTableUnitTestHelper.ClearTable<TableEntity>();

            var entities = AzTableUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.Hundreds_RandomMinValue * 3, ConstProvider.Hundreds_RandomMaxValue * 5);

            // Act
            var _queryAllResponseAct = AzTableUnitTestHelper.QueryAll<TableEntity>();

            // Assert
            AzTableUnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryAllResponseAct, entities,
                (responseValues, originalEntities) => responseValues.Count >= originalEntities.Count());
        }

        [Fact, TestPriority(210)]
        public void QueryByPartitionKeyTest()
        {
            // Arrange
            string commonPartitionKey = Guid.NewGuid().ToString();
            var entities = AzTableUnitTestHelper.CreateAddAssertSomeEntities(commonPartitionKey);
            var others = AzTableUnitTestHelper.CreateAddAssertSomeEntities(Guid.NewGuid().ToString());

            // Act
            var _queryByPartitionKeyResponseAct = AzTableUnitTestHelper.QueryByPartitionKey<TableEntity>(commonPartitionKey);

            // Assert
            AzTableUnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByPartitionKeyResponseAct, entities,
                (responseValues, originalEntities) => responseValues.Count == originalEntities.Count());
        }

        [Fact, TestPriority(210)]
        public void QueryByPartitionKeyStartPatternTest()
        {
            // Arrange
            string commonPartitionKey = Guid.NewGuid().ToString();
            var entities = AzTableUnitTestHelper.CreateAddAssertSomeEntities(commonPartitionKey);
            var others = AzTableUnitTestHelper.CreateAddAssertSomeEntities(Guid.NewGuid().ToString());

            // Act
            var _queryByPartitionKeyStartPatternResponseAct = AzTableUnitTestHelper.QueryByPartitionKeyStartPattern<TableEntity>(
                commonPartitionKey.Substring(0, 5));

            // Assert
            AzTableUnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByPartitionKeyStartPatternResponseAct, entities,
                (responseValues, originalEntities) => responseValues.Count == originalEntities.Count());
        }

        [Fact, TestPriority(210)]
        public void QueryByPartitionKeyRowKeyTest()
        {
            // Arrange
            string commonPartitionKey = Guid.NewGuid().ToString();
            var entities = AzTableUnitTestHelper.CreateAddAssertSomeEntities(commonPartitionKey);
            var selectedEntity = entities.First();

            // Act
            var _queryByPartitionKeyRowKeyResponseAct = AzTableUnitTestHelper.QueryByPartitionKeyRowKey<TableEntity>(
                selectedEntity.PartitionKey, selectedEntity.RowKey);

            // Assert
            AzTableUnitTestHelper.AssertExpectedSuccessfulGenResponse(selectedEntity, _queryByPartitionKeyRowKeyResponseAct);
        }

        [Fact, TestPriority(210)]
        public void QueryByPartitionKeyRowKeyStartPatternTest()
        {
            // Arrange
            string commonPartitionKey = Guid.NewGuid().ToString();
            var entities = AzTableUnitTestHelper.CreateAddAssertSomeEntities(commonPartitionKey); 
            var selectedEntity = entities.First();

            // Act
            var _queryByPartitionKeyRowKeyStartPatternResponseAct = AzTableUnitTestHelper.QueryByPartitionKeyRowKeyStartPattern<TableEntity>(
                selectedEntity.PartitionKey, selectedEntity.RowKey.Substring(0, 5));

            // Assert
            AzTableUnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByPartitionKeyRowKeyStartPatternResponseAct, 
                new List<TableEntity> { selectedEntity },
                (responseValues, originalEntities) => responseValues.Count >= originalEntities.Count());
        }

        [Fact, TestPriority(210)]
        public void QueryByPartitionKeyStartPatternRowKeyStartPatternTest()
        {
            // Arrange
            string commonPartitionKey = Guid.NewGuid().ToString();
            var entities = AzTableUnitTestHelper.CreateAddAssertSomeEntities(commonPartitionKey);
            var selectedEntity = entities.First();

            // Act
            var _queryByPartitionKeyStartPatternRowKeyStartPatternResponseAct = AzTableUnitTestHelper
                .QueryByPartitionKeyStartPatternRowKeyStartPattern<TableEntity>(
                selectedEntity.PartitionKey.Substring(0, 5), selectedEntity.RowKey.Substring(0, 5));

            // Assert
            AzTableUnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByPartitionKeyStartPatternRowKeyStartPatternResponseAct,
                new List<TableEntity> { selectedEntity },
                (responseValues, originalEntities) => responseValues.Count >= originalEntities.Count());
        }

        [Fact, TestPriority(210)]
        public void QueryByTimestampTest()
        {
            // Arrange
            string commonPartitionKey = Guid.NewGuid().ToString();
            AzTableUnitTestHelper.CreateAddAssertSomeEntities(commonPartitionKey);

            var _queryByPartitionKeyResponse = AzTableUnitTestHelper.QueryByPartitionKey<TableEntity>(commonPartitionKey);
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByPartitionKeyResponse);

            var entities = _queryByPartitionKeyResponse.Value.OrderBy(entt => entt.Timestamp).ToList();

            var others = AzTableUnitTestHelper.CreateAddAssertSomeEntities(Guid.NewGuid().ToString());

            // Act
            var _queryByTimestampResponseAct = AzTableUnitTestHelper.QueryByTimestamp<TableEntity>(
                entities.First().Timestamp.Value.UtcDateTime, entities.Last().Timestamp.Value.UtcDateTime);

            // Assert
            AzTableUnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByTimestampResponseAct,
                entities,
                (responseValues, originalEntities) => responseValues.Count >= originalEntities.Count());
        }

        [Fact, TestPriority(210)]
        public void QueryByPartitionKeyTimestampTest()
        {
            // Arrange
            string commonPartitionKey = Guid.NewGuid().ToString();
            AzTableUnitTestHelper.CreateAddAssertSomeEntities(commonPartitionKey);

            var _queryByPartitionKeyResponse = AzTableUnitTestHelper.QueryByPartitionKey<TableEntity>(commonPartitionKey);
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByPartitionKeyResponse);

            var entities = _queryByPartitionKeyResponse.Value.OrderBy(entt => entt.Timestamp).ToList();

            var others = AzTableUnitTestHelper.CreateAddAssertSomeEntities(Guid.NewGuid().ToString());

            // Act
            var _queryByPartitionKeyTimestampResponseAct = AzTableUnitTestHelper.QueryByPartitionKeyTimestamp<TableEntity>(
                commonPartitionKey, entities.First().Timestamp.Value.UtcDateTime, entities.Last().Timestamp.Value.UtcDateTime);

            // Assert
            AzTableUnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByPartitionKeyTimestampResponseAct,
                entities,
                (responseValues, originalEntities) => responseValues.Count >= originalEntities.Count());
        }
    }
}
