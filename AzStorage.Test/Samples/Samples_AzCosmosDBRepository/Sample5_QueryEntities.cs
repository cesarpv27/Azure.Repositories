using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using CoreTools.Extensions;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzCosmosDBRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample5_QueryEntities
    {
        #region QueryAll

        [Fact, TestPriority(10)]
        public void QueryAllTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.RandomMinValue, ConstProvider.RandomMaxValue);

            // Act
            var _queryAllResponseAct = AzCosmosUnitTestHelper.QueryAll<CustomCosmosEntity>();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryAllResponseAct);
            AzCosmosUnitTestHelper.AssertEnumerableBContainsEnumerableAEntities(entities, _queryAllResponseAct.Value);
        }

        [Fact, TestPriority(20)]
        public void LazyQueryAllTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.Hundreds_RandomMinValue, ConstProvider.Hundreds_RandomMaxValue);

            // Act
            var _lazyQueryAllResponseAct = AzCosmosUnitTestHelper.LazyQueryAll<CustomCosmosEntity>();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_lazyQueryAllResponseAct);

            var storedEntities = AzCosmosUnitTestHelper.GetEntitiesFromResponse(_lazyQueryAllResponseAct);
            AzCosmosUnitTestHelper.AssertEnumerableBContainsEnumerableAEntities(entities, storedEntities);
        }

        #endregion

        #region QueryByPartitionKey

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

        [Fact, TestPriority(210)]
        public void QueryByPartitionKeyTest2()
        {
            // Arrange
            string partitionKey = Guid.Empty.ToString();

            // Act
            // Assert
            Assert.Throws<ArgumentException>(() => AzCosmosUnitTestHelper
                .QueryByPartitionKey<CustomCosmosEntity>(partitionKey, "Container2"));
        }

        [Fact, TestPriority(210)]
        public void LazyQueryByPartitionKeyTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.Hundreds_RandomMinValue, ConstProvider.Hundreds_RandomMaxValue);

            string partitionKey = entities.First().PartitionKey;

            // Act
            var _lazyQueryByPartitionKeyResponseAct = AzCosmosUnitTestHelper
                .LazyQueryByPartitionKey<CustomCosmosEntity>(partitionKey);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_lazyQueryByPartitionKeyResponseAct);

            var responseEntities = AzCosmosUnitTestHelper.GetEntitiesFromResponse(_lazyQueryByPartitionKeyResponseAct);

            Assert.Equal(entities.Where(entt => entt.PartitionKey.Equals(partitionKey)).Count(), responseEntities.Count());
            AzCosmosUnitTestHelper.AssertEnumerableBContainsEnumerableAEntities(responseEntities, entities);
        }

        #endregion

        #region QueryByFilter

        [Fact, TestPriority(210)]
        public void QueryByFilterTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.Hundreds_RandomMinValue, ConstProvider.Hundreds_RandomMaxValue);

            string partitionKey = entities.First().PartitionKey;

            string filter = $"select * from Container1 c1 where c1.PartitionKey = '{partitionKey}'";
            // Act
            var _queryByFilterResponseAct = AzCosmosUnitTestHelper.QueryByFilter<CustomCosmosEntity>(filter);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByFilterResponseAct);

            var responseEntities = _queryByFilterResponseAct.Value;

            Assert.Equal(entities.Where(entt => entt.PartitionKey.Equals(partitionKey)).Count(), responseEntities.Count);
            AzCosmosUnitTestHelper.AssertEnumerableBContainsEnumerableAEntities(responseEntities, entities);
        }
        
        [Fact, TestPriority(210)]
        public void QueryByFilterTest2()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.Hundreds_RandomMinValue, ConstProvider.Hundreds_RandomMaxValue);

            string partitionKey1 = entities.First().PartitionKey;
            string partitionKey2 = entities.Where(entt => !entt.PartitionKey.Equals(partitionKey1)).First().PartitionKey;

            string filter = $"select * from C c1 where c1.PartitionKey = '{partitionKey1}'" +
                $" or c1.PartitionKey = '{partitionKey2}'";
            // Act
            var _queryByFilterResponseAct = AzCosmosUnitTestHelper.QueryByFilter<CustomCosmosEntity>(filter);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByFilterResponseAct);

            var responseEntities = _queryByFilterResponseAct.Value;

            Assert.Equal(entities.Where(
                entt => entt.PartitionKey.Equals(partitionKey1) || entt.PartitionKey.Equals(partitionKey2))
                .Count(), responseEntities.Count);
            AzCosmosUnitTestHelper.AssertEnumerableBContainsEnumerableAEntities(responseEntities, entities);
        }

        [Fact, TestPriority(210)]
        public void LazyQueryByFilter()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.Hundreds_RandomMinValue, ConstProvider.Hundreds_RandomMaxValue);

            string partitionKey = entities.First().PartitionKey;

            string filter = $"select * from C c1 where c1.PartitionKey = '{partitionKey}'";
            // Act
            var _lazyQueryByFilterResponseAct = AzCosmosUnitTestHelper.LazyQueryByFilter<CustomCosmosEntity>(filter);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_lazyQueryByFilterResponseAct);

            var responseEntities = AzCosmosUnitTestHelper.GetEntitiesFromResponse(_lazyQueryByFilterResponseAct);

            Assert.Equal(entities.Where(entt => entt.PartitionKey.Equals(partitionKey)).Count(), responseEntities.Count());
            AzCosmosUnitTestHelper.AssertEnumerableBContainsEnumerableAEntities(responseEntities, entities);
        }

        //[Fact, TestPriority(210)]
        //public void LazyQueryByFilterTest2()
        //{
        //    // Arrange
        //    var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(true,
        //        ConstProvider.Thousands_RandomMinValue, ConstProvider.Thousands_RandomMaxValue);

        //    string partitionKey = entities.First().PartitionKey;

        //    string filter = $"select * from C c1 where c1.PartitionKey = '{partitionKey}'";
        //    // Act
        //    var _lazyQueryByFilterResponseAct = AzCosmosUnitTestHelper.LazyQueryByFilter<CustomCosmosEntity>(filter);

        //    // Assert
        //    UnitTestHelper.AssertExpectedSuccessfulGenResponse(_lazyQueryByFilterResponseAct);

        //    var responseEntities = AzCosmosUnitTestHelper.GetEntitiesFromResponse(_lazyQueryByFilterResponseAct);

        //    Assert.Equal(entities.Where(entt => entt.PartitionKey.Equals(partitionKey)).Count(), responseEntities.Count());
        //    AzCosmosUnitTestHelper.AssertEnumerableBContainsEnumerableAEntities(responseEntities, entities);
        //}

        #endregion

        #region QueryByQueryDefinition

        [Fact, TestPriority(210)]
        public void QueryByQueryDefinitionTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.Hundreds_RandomMinValue, ConstProvider.Hundreds_RandomMaxValue);

            string partitionKey = entities.First().PartitionKey;

            string queryText = $"select * from C c1 where c1.PartitionKey = @PartitionKey";
            var queryDefinition = new QueryDefinition(queryText).WithParameter("@PartitionKey", partitionKey);

            // Act
            var _queryByQueryDefinitionResponseAct = AzCosmosUnitTestHelper.QueryByQueryDefinition<CustomCosmosEntity>(queryDefinition);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByQueryDefinitionResponseAct);

            var responseEntities = _queryByQueryDefinitionResponseAct.Value;

            Assert.Equal(entities.Where(entt => entt.PartitionKey.Equals(partitionKey)).Count(), responseEntities.Count());
            AzCosmosUnitTestHelper.AssertEnumerableBContainsEnumerableAEntities(responseEntities, entities);
        }
        
        [Fact, TestPriority(210)]
        public void LazyQueryByQueryDefinitionTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.Hundreds_RandomMinValue, ConstProvider.Hundreds_RandomMaxValue);

            string partitionKey = entities.First().PartitionKey;

            string queryText = $"select * from C c1 where c1.PartitionKey = @PartitionKey";
            var queryDefinition = new QueryDefinition(queryText).WithParameter("@PartitionKey", partitionKey);

            // Act
            var _lazyQueryByQueryDefinitionResponseAct = AzCosmosUnitTestHelper.LazyQueryByQueryDefinition<CustomCosmosEntity>(queryDefinition);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_lazyQueryByQueryDefinitionResponseAct);

            var responseEntities = AzCosmosUnitTestHelper.GetEntitiesFromResponse(_lazyQueryByQueryDefinitionResponseAct);

            Assert.Equal(entities.Where(entt => entt.PartitionKey.Equals(partitionKey)).Count(), responseEntities.Count());
            AzCosmosUnitTestHelper.AssertEnumerableBContainsEnumerableAEntities(responseEntities, entities);
        }

        #endregion
    }
}
