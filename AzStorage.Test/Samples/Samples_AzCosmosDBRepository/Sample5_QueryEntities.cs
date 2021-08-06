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
            AzCosmosUnitTestHelper.CommonQueryAllTest<CustomCosmosEntity, List<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.QueryAll<CustomCosmosEntity>);
        }

        [Fact, TestPriority(20)]
        public void LazyQueryAllTest()
        {
            AzCosmosUnitTestHelper.CommonQueryAllTest<CustomCosmosEntity, IEnumerable<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.LazyQueryAll<CustomCosmosEntity>);
        }

        #endregion

        #region QueryByPartitionKey

        [Fact, TestPriority(210)]
        public void QueryByPartitionKeyTest()
        {
            AzCosmosUnitTestHelper.CommonQueryByPartitionKeyTest<CustomCosmosEntity, List<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.QueryByPartitionKey<CustomCosmosEntity>);
        }

        [Fact, TestPriority(210)]
        public void LazyQueryByPartitionKeyTest()
        {
            AzCosmosUnitTestHelper.CommonQueryByPartitionKeyTest<CustomCosmosEntity, IEnumerable<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.LazyQueryByPartitionKey<CustomCosmosEntity>);
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

        #endregion

        #region QueryByFilter

        [Fact, TestPriority(210)]
        public void QueryByFilterTest()
        {
            AzCosmosUnitTestHelper.CommonQueryByFilterTest<CustomCosmosEntity, List<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.QueryByFilter<CustomCosmosEntity>);
        }

        [Fact, TestPriority(210)]
        public void LazyQueryByFilter()
        {
            AzCosmosUnitTestHelper.CommonQueryByFilterTest<CustomCosmosEntity, IEnumerable<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.LazyQueryByFilter<CustomCosmosEntity>);
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
            AzCosmosUnitTestHelper.CommonQueryByQueryDefinitionTest<CustomCosmosEntity, List<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.QueryByQueryDefinition<CustomCosmosEntity>);
        }
        
        [Fact, TestPriority(210)]
        public void LazyQueryByQueryDefinitionTest()
        {
            AzCosmosUnitTestHelper.CommonQueryByQueryDefinitionTest<CustomCosmosEntity, IEnumerable<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.LazyQueryByQueryDefinition<CustomCosmosEntity>);
        }

        #endregion

        #region QueryWithOr & Lazy

        [Fact, TestPriority(210)]
        public void QueryWithOrTest()
        {
            AzCosmosUnitTestHelper.CommonQueryWithAndOrTest<CustomCosmosEntity, List<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.QueryWithOr<CustomCosmosEntity>, CoreTools.Utilities.BooleanOperator.or);
        }
        
        [Fact, TestPriority(210)]
        public void LazyQueryWithOrTest()
        {
            AzCosmosUnitTestHelper.CommonQueryWithAndOrTest<CustomCosmosEntity, IEnumerable<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.LazyQueryWithOr<CustomCosmosEntity>, CoreTools.Utilities.BooleanOperator.or);
        }
        
        [Fact, TestPriority(210)]
        public void QueryWithOrTest2()
        {
            AzCosmosUnitTestHelper.CommonQueryWithAndOrTest2<CustomCosmosEntity, List<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.QueryWithOr<CustomCosmosEntity>, CoreTools.Utilities.BooleanOperator.or);
        }

        [Fact, TestPriority(210)]
        public void LazyQueryWithOrTest2()
        {
            AzCosmosUnitTestHelper.CommonQueryWithAndOrTest2<CustomCosmosEntity, IEnumerable<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.LazyQueryWithOr<CustomCosmosEntity>, CoreTools.Utilities.BooleanOperator.or);
        }

        #endregion

        #region QueryWithAnd & Lazy

        [Fact, TestPriority(210)]
        public void QueryWithAndTest()
        {
            AzCosmosUnitTestHelper.CommonQueryWithAndOrTest<CustomCosmosEntity, List<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.QueryWithAnd<CustomCosmosEntity>, CoreTools.Utilities.BooleanOperator.and);
        }
        
        [Fact, TestPriority(210)]
        public void LazyQueryWithAndTest()
        {
            AzCosmosUnitTestHelper.CommonQueryWithAndOrTest<CustomCosmosEntity, IEnumerable<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.LazyQueryWithAnd<CustomCosmosEntity>, CoreTools.Utilities.BooleanOperator.and);
        }
        
        [Fact, TestPriority(210)]
        public void QueryWithAndTest2()
        {
            AzCosmosUnitTestHelper.CommonQueryWithAndOrTest2<CustomCosmosEntity, List<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.QueryWithAnd<CustomCosmosEntity>, CoreTools.Utilities.BooleanOperator.and);
        }
        
        [Fact, TestPriority(210)]
        public void LazyQueryWithAndTest2()
        {
            AzCosmosUnitTestHelper.CommonQueryWithAndOrTest2<CustomCosmosEntity, IEnumerable<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.LazyQueryWithAnd<CustomCosmosEntity>, CoreTools.Utilities.BooleanOperator.and);
        }

        #endregion
    }
}
