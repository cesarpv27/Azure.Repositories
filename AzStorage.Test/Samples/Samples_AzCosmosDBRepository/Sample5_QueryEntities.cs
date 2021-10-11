using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AzCoreTools.Core;
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

        [Fact, TestPriority(50)]
        public void QueryAllTest()
        {
            AzCosmosUnitTestHelper.CommonQueryAllTest<CustomCosmosEntity, List<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.QueryAll<CustomCosmosEntity>);
        }

        [Fact, TestPriority(52)]
        public void LazyQueryAllTest()
        {
            AzCosmosUnitTestHelper.CommonQueryAllTest<CustomCosmosEntity, IEnumerable<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.LazyQueryAll<CustomCosmosEntity>);
        }

        [Fact, TestPriority(54)]
        public void QueryAllAsyncTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.Hundreds_RandomMinValue, ConstProvider.Hundreds_RandomMaxValue);

            // Act
            var _queryAllResponseAct = AzCosmosUnitTestHelper
                .QueryAllAsync<CustomCosmosEntity>(CreateResourcePolicy.OnlyFirstTime)
                .WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryAllResponseAct);

            var storedEntities = _queryAllResponseAct.Value.ToList();
            AzCosmosUnitTestHelper.AssertEnumerableBContainsEnumerableAEntities(entities, storedEntities);
        }

        [Fact, TestPriority(56)]
        public void QueryAllWithContinuationTokenTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.Hundreds_RandomMinValue, ConstProvider.Hundreds_RandomMaxValue);
            int take1 = 100;
            int take2 = 100;

            // Act
            var _queryAllResponseAct1 = AzCosmosUnitTestHelper.QueryAll<CustomCosmosEntity>(take1);
            var _queryAllResponseAct2 = AzCosmosUnitTestHelper.QueryAll<CustomCosmosEntity>(
                take2, _queryAllResponseAct1.ContinuationToken);
            var _queryAllResponseAct3 = AzCosmosUnitTestHelper.QueryAll<CustomCosmosEntity>(take1 + take2);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryAllResponseAct1);
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryAllResponseAct2);
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryAllResponseAct3);

            Assert.NotEmpty(_queryAllResponseAct1.ContinuationToken);
            Assert.NotEmpty(_queryAllResponseAct2.ContinuationToken);
            Assert.NotEmpty(_queryAllResponseAct3.ContinuationToken);

            var storedEntities1 = _queryAllResponseAct1.Value.ToList();
            var storedEntities2 = _queryAllResponseAct2.Value.ToList();
            var storedEntities3 = _queryAllResponseAct3.Value.ToList();

            AzCosmosUnitTestHelper.AssertEnumerableBContainsEnumerableAEntities(storedEntities1, storedEntities3);
            AzCosmosUnitTestHelper.AssertEnumerableBContainsEnumerableAEntities(storedEntities2, storedEntities3);

            AzCosmosUnitTestHelper.AssertEnumerableBNotContainsAnyEnumerableAEntities(storedEntities1, storedEntities2);
        }

        #endregion

        #region QueryByPartitionKey

        [Fact, TestPriority(500)]
        public void QueryByPartitionKeyTest()
        {
            AzCosmosUnitTestHelper.CommonQueryByPartitionKeyTest<CustomCosmosEntity, List<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.QueryByPartitionKey<CustomCosmosEntity>);
        }

        [Fact, TestPriority(500)]
        public void LazyQueryByPartitionKeyTest()
        {
            AzCosmosUnitTestHelper.CommonQueryByPartitionKeyTest<CustomCosmosEntity, IEnumerable<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.LazyQueryByPartitionKey<CustomCosmosEntity>);
        }

        [Fact, TestPriority(500)]
        public void QueryByPartitionKeyTest2()
        {
            // Arrange
            string partitionKey = Guid.Empty.ToString();

            // Act
            // Assert
            Assert.Throws<ArgumentException>(() => AzCosmosUnitTestHelper
                .QueryByPartitionKey<CustomCosmosEntity>(partitionKey, "Container2"));
        }

        [Fact, TestPriority(500)]
        public void QueryByPartitionKeyAsyncTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.Hundreds_RandomMinValue, ConstProvider.Hundreds_RandomMaxValue);

            string partitionKey = entities.First().PartitionKey;

            // Act
            var _queryByPartitionKeyResponseAct = AzCosmosUnitTestHelper
                .QueryByPartitionKeyAsync<CustomCosmosEntity>(partitionKey, null, null, CreateResourcePolicy.OnlyFirstTime)
                .WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByPartitionKeyResponseAct);

            var responseEntities = _queryByPartitionKeyResponseAct.Value.ToList();

            Assert.Equal(entities.Where(entt => entt.PartitionKey.Equals(partitionKey)).Count(), responseEntities.Count);
            AzCosmosUnitTestHelper.AssertEnumerableBContainsEnumerableAEntities(responseEntities, entities);
        }

        #endregion

        #region QueryByFilter

        [Fact, TestPriority(510)]
        public void QueryByFilterTest()
        {
            AzCosmosUnitTestHelper.CommonQueryByFilterTest<CustomCosmosEntity, List<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.QueryByFilter<CustomCosmosEntity>);
        }

        [Fact, TestPriority(510)]
        public void LazyQueryByFilter()
        {
            AzCosmosUnitTestHelper.CommonQueryByFilterTest<CustomCosmosEntity, IEnumerable<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.LazyQueryByFilter<CustomCosmosEntity>);
        }

        [Fact, TestPriority(510)]
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

        //[Fact, TestPriority(510)]
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

        [Fact, TestPriority(510)]
        public void QueryByFilterAsyncTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.Hundreds_RandomMinValue, ConstProvider.Hundreds_RandomMaxValue);

            string partitionKey = entities.First().PartitionKey;

            string filter = $"select * from Container c1 where c1.PartitionKey = '{partitionKey}'";

            // Act
            var _queryByFilterResponseAct = AzCosmosUnitTestHelper
                .QueryByFilterAsync<CustomCosmosEntity>(filter, null, null, CreateResourcePolicy.OnlyFirstTime)
                .WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByFilterResponseAct);

            var responseEntities = _queryByFilterResponseAct.Value.ToList();

            Assert.Equal(entities.Where(entt => entt.PartitionKey.Equals(partitionKey)).Count(), responseEntities.Count);
            AzCosmosUnitTestHelper.AssertEnumerableBContainsEnumerableAEntities(responseEntities, entities);
        }

        #endregion

        #region QueryByQueryDefinition

        [Fact, TestPriority(520)]
        public void QueryByQueryDefinitionTest()
        {
            AzCosmosUnitTestHelper.CommonQueryByQueryDefinitionTest<CustomCosmosEntity, List<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.QueryByQueryDefinition<CustomCosmosEntity>);
        }
        
        [Fact, TestPriority(520)]
        public void LazyQueryByQueryDefinitionTest()
        {
            AzCosmosUnitTestHelper.CommonQueryByQueryDefinitionTest<CustomCosmosEntity, IEnumerable<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.LazyQueryByQueryDefinition<CustomCosmosEntity>);
        }

        [Fact, TestPriority(520)]
        public void QueryByQueryDefinitionAsyncTest()
        {
            // Arrange
            var entities = AzCosmosUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.Hundreds_RandomMinValue, ConstProvider.Hundreds_RandomMaxValue);

            string partitionKey = entities.First().PartitionKey;

            string queryText = $"select * from Container c1 where c1.PartitionKey = @PartitionKey";
            var queryDefinition = new QueryDefinition(queryText).WithParameter("@PartitionKey", partitionKey);

            // Act
            var _queryByQueryDefinitionResponseAct = AzCosmosUnitTestHelper
                .QueryByQueryDefinitionAsync<CustomCosmosEntity>(queryDefinition, CreateResourcePolicy.OnlyFirstTime)
                .WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryByQueryDefinitionResponseAct);

            var responseEntities = _queryByQueryDefinitionResponseAct.Value.ToList();

            Assert.Equal(entities.Where(entt => entt.PartitionKey.Equals(partitionKey)).Count(), responseEntities.Count());
            AzCosmosUnitTestHelper.AssertEnumerableBContainsEnumerableAEntities(responseEntities, entities);
        }

        #endregion

        #region QueryWithOr & Lazy

        [Fact, TestPriority(530)]
        public void QueryWithOrTest()
        {
            AzCosmosUnitTestHelper.CommonQueryWithAndOrTest<CustomCosmosEntity, List<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.QueryWithOr<CustomCosmosEntity>, CoreTools.Utilities.BooleanOperator.or);
        }
        
        [Fact, TestPriority(530)]
        public void LazyQueryWithOrTest()
        {
            AzCosmosUnitTestHelper.CommonQueryWithAndOrTest<CustomCosmosEntity, IEnumerable<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.LazyQueryWithOr<CustomCosmosEntity>, CoreTools.Utilities.BooleanOperator.or);
        }
        
        [Fact, TestPriority(530)]
        public void QueryWithOrTest2()
        {
            AzCosmosUnitTestHelper.CommonQueryWithAndOrTest2<CustomCosmosEntity, List<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.QueryWithOr<CustomCosmosEntity>, CoreTools.Utilities.BooleanOperator.or);
        }

        [Fact, TestPriority(530)]
        public void LazyQueryWithOrTest2()
        {
            AzCosmosUnitTestHelper.CommonQueryWithAndOrTest2<CustomCosmosEntity, IEnumerable<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.LazyQueryWithOr<CustomCosmosEntity>, CoreTools.Utilities.BooleanOperator.or);
        }

        [Fact, TestPriority(530)]
        public void QueryWithOrAsyncTest()
        {
            // Arrange
            var r = new Random();

            var amount = r.Next(ConstProvider.Hundreds_RandomMinValue, ConstProvider.Hundreds_RandomMaxValue);
            var entities = AzCosmosUnitTestHelper.CreateSomeEntities(amount, true);

            foreach (var item in entities)
            {
                item.Prop1 = r.Next(1, 4).ToString();
                item.Prop2 = r.Next(1, 4).ToString();
            }

            AzCosmosUnitTestHelper.AddAssertSomeEntities(entities);

            var prop1QueryValue = "1";
            var prop2QueryValue = "2";

            // Act
            var _queryWithAndOrResponseAct = AzCosmosUnitTestHelper
                .QueryWithOrAsync<CustomCosmosEntity>(
                new
                {
                    Prop1 = prop1QueryValue,
                    Prop2 = prop2QueryValue
                }, null, null, CreateResourcePolicy.OnlyFirstTime)
                .WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryWithAndOrResponseAct);

            var locallyQueriedEntities = entities.Where(entt => entt.Prop1.Equals(prop1QueryValue) || entt.Prop2.Equals(prop2QueryValue));
            
            var responseEntities = _queryWithAndOrResponseAct.Value.ToList();

            Assert.True(responseEntities.Count() >= locallyQueriedEntities.Count());
            AzCosmosUnitTestHelper.AssertEnumerableBContainsEnumerableAEntities(locallyQueriedEntities, responseEntities);
        }

        #endregion

        #region QueryWithAnd & Lazy

        [Fact, TestPriority(540)]
        public void QueryWithAndTest()
        {
            AzCosmosUnitTestHelper.CommonQueryWithAndOrTest<CustomCosmosEntity, List<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.QueryWithAnd<CustomCosmosEntity>, CoreTools.Utilities.BooleanOperator.and);
        }
        
        [Fact, TestPriority(540)]
        public void LazyQueryWithAndTest()
        {
            AzCosmosUnitTestHelper.CommonQueryWithAndOrTest<CustomCosmosEntity, IEnumerable<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.LazyQueryWithAnd<CustomCosmosEntity>, CoreTools.Utilities.BooleanOperator.and);
        }
        
        [Fact, TestPriority(500)]
        public void QueryWithAndTest2()
        {
            AzCosmosUnitTestHelper.CommonQueryWithAndOrTest2<CustomCosmosEntity, List<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.QueryWithAnd<CustomCosmosEntity>, CoreTools.Utilities.BooleanOperator.and);
        }
        
        [Fact, TestPriority(540)]
        public void LazyQueryWithAndTest2()
        {
            AzCosmosUnitTestHelper.CommonQueryWithAndOrTest2<CustomCosmosEntity, IEnumerable<CustomCosmosEntity>>(
                AzCosmosUnitTestHelper.LazyQueryWithAnd<CustomCosmosEntity>, CoreTools.Utilities.BooleanOperator.and);
        }

        [Fact, TestPriority(540)]
        public void QueryWithAndAsyncTest()
        {
            // Arrange
            var r = new Random();

            var amount = r.Next(ConstProvider.Hundreds_RandomMinValue, ConstProvider.Hundreds_RandomMaxValue);
            var entities = AzCosmosUnitTestHelper.CreateSomeEntities(amount, true);

            foreach (var item in entities)
            {
                item.Prop1 = r.Next(1, 4).ToString();
                item.Prop2 = r.Next(1, 4).ToString();
            }

            AzCosmosUnitTestHelper.AddAssertSomeEntities(entities);

            var prop1QueryValue = "1";
            var prop2QueryValue = "2";

            // Act
            var _queryWithAndOrResponseAct = AzCosmosUnitTestHelper
                .QueryWithAndAsync<CustomCosmosEntity>(
                new
                {
                    Prop1 = prop1QueryValue,
                    Prop2 = prop2QueryValue
                }, null, null, CreateResourcePolicy.OnlyFirstTime)
                .WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_queryWithAndOrResponseAct);

            var locallyQueriedEntities = entities.Where(entt => entt.Prop1.Equals(prop1QueryValue) && entt.Prop2.Equals(prop2QueryValue));

            var responseEntities = _queryWithAndOrResponseAct.Value.ToList();

            Assert.True(responseEntities.Count() >= locallyQueriedEntities.Count());
            AzCosmosUnitTestHelper.AssertEnumerableBContainsEnumerableAEntities(locallyQueriedEntities, responseEntities);
        }

        #endregion
    }
}
