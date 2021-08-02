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

        [Fact, TestPriority(110)]
        public void LazyQueryAllTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.Hundreds_RandomMinValue, ConstProvider.Hundreds_RandomMaxValue);

            // Act
            var _lazyQueryAllResponseAct = AzCosmosUnitTestHelper.LazyQueryAll<CustomCosmosEntity>();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_lazyQueryAllResponseAct);

            Assert.True(_lazyQueryAllResponseAct.Value.Count() >= entities.Count());
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

            //var enttAmount = 0;
            //foreach (var item in _lazyQueryByPartitionKeyResponseAct.Value)
            //    enttAmount++;

            Assert.Equal(entities.Where(entt => entt.PartitionKey.Equals(partitionKey)).Count(), _lazyQueryByPartitionKeyResponseAct.Value.Count());
        }

        //[Fact, TestPriority(210)]
        //public void LazyQueryByPartitionKey2Test()
        //{
        //    // Arrange
        //    var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(false,
        //        ConstProvider.Thousands_RandomMinValue, ConstProvider.Thousands_RandomMaxValue);

        //    string partitionKey = entities.First().PartitionKey;

        //    // Act
        //    var _lazyQueryByPartitionKeyResponseAct = AzCosmosUnitTestHelper.LazyQueryByPartitionKey<CustomCosmosEntity>(partitionKey);

        //    // Assert
        //    UnitTestHelper.AssertExpectedSuccessfulGenResponse(_lazyQueryByPartitionKeyResponseAct);

        //    Assert.Equal(entities.Where(entt => entt.PartitionKey.Equals(partitionKey)).Count(), _lazyQueryByPartitionKeyResponseAct.Value.Count());
        //}

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

            var enttAmount = 0;
            foreach (var item in _queryByFilterResponseAct.Value)
                enttAmount++;

            Assert.Equal(entities.Where(entt => entt.PartitionKey.Equals(partitionKey)).Count(), enttAmount);
        }
        
        [Fact, TestPriority(210)]
        public void LazyQueryByFilter()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.Hundreds_RandomMinValue, ConstProvider.Hundreds_RandomMaxValue);

            string partitionKey = entities.First().PartitionKey;

            string filter = $"select * from Container1 c1 where c1.PartitionKey = '{partitionKey}'";
            // Act
            var _lazyQueryByFilterResponseAct = AzCosmosUnitTestHelper.LazyQueryByFilter<CustomCosmosEntity>(filter);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_lazyQueryByFilterResponseAct);

            var enttAmount = 0;
            foreach (var item in _lazyQueryByFilterResponseAct.Value)
                enttAmount++;

            Assert.Equal(entities.Where(entt => entt.PartitionKey.Equals(partitionKey)).Count(), enttAmount);
        }

        //[Fact, TestPriority(210)]
        //public void QueryByFilterTest2()
        //{
        //    Arrange
        //   var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(true,
        //       ConstProvider.Thousands_RandomMinValue, ConstProvider.Thousands_RandomMaxValue);

        //    string partitionKey = entities.First().PartitionKey;

        //    string filter = $"select * from Container1 c1 where c1.PartitionKey = '{partitionKey}'";
        //    Act
        //   var _queryByFilterResponseAct = AzCosmosUnitTestHelper.LazyQueryByFilter<CustomCosmosEntity>(filter);

        //    Assert
        //    UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByFilterResponseAct);

        //    Assert.Equal(entities.Where(entt => entt.PartitionKey.Equals(partitionKey)).Count(), queryCount);
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

            string queryText = $"select * from Container1 c1 where c1.PartitionKey = @PartitionKey";
            var queryDefinition = new QueryDefinition(queryText).WithParameter("@PartitionKey", partitionKey);

            // Act
            var _queryByQueryDefinitionResponseAct = AzCosmosUnitTestHelper.QueryByQueryDefinition<CustomCosmosEntity>(queryDefinition);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByQueryDefinitionResponseAct);

            var enttAmount = 0;
            foreach (var item in _queryByQueryDefinitionResponseAct.Value)
                enttAmount++;

            Assert.Equal(entities.Where(entt => entt.PartitionKey.Equals(partitionKey)).Count(), enttAmount);
        }
        
        [Fact, TestPriority(210)]
        public void LazyQueryByQueryDefinitionTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.Hundreds_RandomMinValue, ConstProvider.Hundreds_RandomMaxValue);

            string partitionKey = entities.First().PartitionKey;

            string queryText = $"select * from Container1 c1 where c1.PartitionKey = @PartitionKey";
            var queryDefinition = new QueryDefinition(queryText).WithParameter("@PartitionKey", partitionKey);

            // Act
            var _queryByQueryDefinitionResponseAct = AzCosmosUnitTestHelper.LazyQueryByQueryDefinition<CustomCosmosEntity>(queryDefinition);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByQueryDefinitionResponseAct);

            var enttAmount = 0;
            foreach (var item in _queryByQueryDefinitionResponseAct.Value)
                enttAmount++;

            Assert.Equal(entities.Where(entt => entt.PartitionKey.Equals(partitionKey)).Count(), enttAmount);
        }

        #endregion
    }
}
