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
            UnitTestHelper.ClearTable<TableEntity>();

            var entities = UnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.Hundreds_RandomMinValue * 3, ConstProvider.Hundreds_RandomMaxValue * 5);

            // Act
            var _queryAllResponseAct = UnitTestHelper.QueryAll<TableEntity>();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryAllResponseAct, entities,
                (responseValues, originalEntities) => responseValues.Count >= originalEntities.Count());
        }

        [Fact, TestPriority(210)]
        public void QueryByPartitionKeyTest()
        {
            // Arrange
            string commonPartitionKey = Guid.NewGuid().ToString();
            var entities = UnitTestHelper.CreateAddAssertSomeEntities(commonPartitionKey);
            var others = UnitTestHelper.CreateAddAssertSomeEntities(Guid.NewGuid().ToString());

            // Act
            var _queryByPartitionKeyResponseAct = UnitTestHelper.QueryByPartitionKey<TableEntity>(commonPartitionKey);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByPartitionKeyResponseAct, entities,
                (responseValues, originalEntities) => responseValues.Count == originalEntities.Count());
        }

        [Fact, TestPriority(210)]
        public void QueryByPartitionKeyStartPatternTest()
        {
            // Arrange
            string commonPartitionKey = Guid.NewGuid().ToString();
            var entities = UnitTestHelper.CreateAddAssertSomeEntities(commonPartitionKey);
            var others = UnitTestHelper.CreateAddAssertSomeEntities(Guid.NewGuid().ToString());

            // Act
            var _queryByPartitionKeyStartPatternResponseAct = UnitTestHelper.QueryByPartitionKeyStartPattern<TableEntity>(
                commonPartitionKey.Substring(0, 5));

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByPartitionKeyStartPatternResponseAct, entities,
                (responseValues, originalEntities) => responseValues.Count == originalEntities.Count());
        }

        [Fact, TestPriority(210)]
        public void QueryByPartitionKeyRowKeyTest()
        {
            // Arrange
            string commonPartitionKey = Guid.NewGuid().ToString();
            var entities = UnitTestHelper.CreateAddAssertSomeEntities(commonPartitionKey);
            var selectedEntity = entities.First();

            // Act
            var _queryByPartitionKeyRowKeyResponseAct = UnitTestHelper.QueryByPartitionKeyRowKey<TableEntity>(
                selectedEntity.PartitionKey, selectedEntity.RowKey);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(selectedEntity, _queryByPartitionKeyRowKeyResponseAct);
        }

        [Fact, TestPriority(210)]
        public void QueryByPartitionKeyRowKeyStartPatternTest()
        {
            // Arrange
            string commonPartitionKey = Guid.NewGuid().ToString();
            var entities = UnitTestHelper.CreateAddAssertSomeEntities(commonPartitionKey); 
            var selectedEntity = entities.First();

            // Act
            var _queryByPartitionKeyRowKeyStartPatternResponseAct = UnitTestHelper.QueryByPartitionKeyRowKeyStartPattern<TableEntity>(
                selectedEntity.PartitionKey, selectedEntity.RowKey.Substring(0, 5));

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByPartitionKeyRowKeyStartPatternResponseAct, 
                new List<TableEntity> { selectedEntity },
                (responseValues, originalEntities) => responseValues.Count >= originalEntities.Count());
        }

        [Fact, TestPriority(210)]
        public void QueryByPartitionKeyStartPatternRowKeyStartPatternTest()
        {
            // Arrange
            string commonPartitionKey = Guid.NewGuid().ToString();
            var entities = UnitTestHelper.CreateAddAssertSomeEntities(commonPartitionKey);
            var selectedEntity = entities.First();

            // Act
            var _queryByPartitionKeyStartPatternRowKeyStartPatternResponseAct = UnitTestHelper
                .QueryByPartitionKeyStartPatternRowKeyStartPattern<TableEntity>(
                selectedEntity.PartitionKey.Substring(0, 5), selectedEntity.RowKey.Substring(0, 5));

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByPartitionKeyStartPatternRowKeyStartPatternResponseAct,
                new List<TableEntity> { selectedEntity },
                (responseValues, originalEntities) => responseValues.Count >= originalEntities.Count());
        }

        [Fact, TestPriority(210)]
        public void QueryByTimestampTest()
        {
            // Arrange
            string commonPartitionKey = Guid.NewGuid().ToString();
            UnitTestHelper.CreateAddAssertSomeEntities(commonPartitionKey);

            var _queryByPartitionKeyResponse = UnitTestHelper.QueryByPartitionKey<TableEntity>(commonPartitionKey);
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByPartitionKeyResponse);

            var entities = _queryByPartitionKeyResponse.Value.OrderBy(entt => entt.Timestamp).ToList();

            var others = UnitTestHelper.CreateAddAssertSomeEntities(Guid.NewGuid().ToString());

            // Act
            var _queryByTimestampResponseAct = UnitTestHelper.QueryByTimestamp<TableEntity>(
                entities.First().Timestamp.Value.UtcDateTime, entities.Last().Timestamp.Value.UtcDateTime);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByTimestampResponseAct,
                entities,
                (responseValues, originalEntities) => responseValues.Count >= originalEntities.Count());
        }

        [Fact, TestPriority(210)]
        public void QueryByPartitionKeyTimestampTest()
        {
            // Arrange
            string commonPartitionKey = Guid.NewGuid().ToString();
            UnitTestHelper.CreateAddAssertSomeEntities(commonPartitionKey);

            var _queryByPartitionKeyResponse = UnitTestHelper.QueryByPartitionKey<TableEntity>(commonPartitionKey);
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByPartitionKeyResponse);

            var entities = _queryByPartitionKeyResponse.Value.OrderBy(entt => entt.Timestamp).ToList();

            var others = UnitTestHelper.CreateAddAssertSomeEntities(Guid.NewGuid().ToString());

            // Act
            var _queryByPartitionKeyTimestampResponseAct = UnitTestHelper.QueryByPartitionKeyTimestamp<TableEntity>(
                commonPartitionKey, entities.First().Timestamp.Value.UtcDateTime, entities.Last().Timestamp.Value.UtcDateTime);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByPartitionKeyTimestampResponseAct,
                entities,
                (responseValues, originalEntities) => responseValues.Count >= originalEntities.Count());
        }
    }
}
