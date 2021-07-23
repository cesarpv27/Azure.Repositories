using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using CoreTools.Extensions;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzCosmosDBRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample5_QueryEntities
    {
        [Fact, TestPriority(100)]
        public void QueryAllTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.RandomMinValue, ConstProvider.RandomMaxValue);

            // Act
            var _queryAllResponseAct = AzCosmosUnitTestHelper.QueryAll<CustomCosmosEntity>();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryAllResponseAct);
            Assert.Equal(entities.Count(), _queryAllResponseAct.Value.Count);
        }

        [Fact, TestPriority(210)]
        public void QueryByPartitionKeyTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.Hundreds_RandomMinValue, ConstProvider.Hundreds_RandomMaxValue);

            string partitionKey = entities.First().PartitionKey;

            // Act
            var _queryAllResponseAct = AzCosmosUnitTestHelper.QueryByPartitionKey<CustomCosmosEntity>(partitionKey);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryAllResponseAct);
            Assert.Equal(entities.Where(entt => entt.PartitionKey.Equals(partitionKey)).Count(), _queryAllResponseAct.Value.Count);
        }
    }
}
