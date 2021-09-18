using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using Xunit;
using CoreTools.Helpers;

namespace AzStorage.Test.Samples.Samples_AzTableRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample7_TransactionalOperations
    {
        #region Add entities transactionally (sync & async)

        [Fact, TestPriority(700)]
        public void AddEntitiesTransactionallyTest()
        {
            // Arrange
            var entities = AzTableUnitTestHelper.CreateSomeEntities(AzTableUnitTestHelper.GetOverOneHundredRandomValue(), true);

            // Act
            var _addEntitiesTransactionallyResponsesAct = AzTableUnitTestHelper.AddEntitiesTransactionally(entities);

            // Assert
            AzTableUnitTestHelper.AssertSucceededResponses(_addEntitiesTransactionallyResponsesAct);

            //AzTableUnitTestHelper.AssertByGetEntity(entities);
        }

        [Fact, TestPriority(700)]
        public void AddExistingEntitiesTransactionallyTest()
        {
            // Arrange
            var entities = AzTableUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.Hundreds_RandomMinValue, ConstProvider.Hundreds_RandomMaxValue);

            // Act
            var _addEntitiesTransactionallyResponsesAct = AzTableUnitTestHelper.AddEntitiesTransactionally(entities);

            // Assert
            AzTableUnitTestHelper.AssertExpectedFailedResponses(_addEntitiesTransactionallyResponsesAct, ConstProvider.The_specified_entity_already_exists);
        }

        [Fact, TestPriority(700)]
        public void AddEntitiesTransactionallyAsyncTest()
        {
            // Arrange
            var entities = AzTableUnitTestHelper.CreateSomeEntities(AzTableUnitTestHelper.GetOverOneHundredRandomValue(), true);

            // Act
            var _addEntitiesTransactionallyAsyncResponseAct = Helper.RunAsSync(entities, AzTableUnitTestHelper.AddEntitiesTransactionallyAsync);

            // Assert
            AzTableUnitTestHelper.AssertSucceededResponses(_addEntitiesTransactionallyAsyncResponseAct);

            //AzTableUnitTestHelper.AssertByGetEntity(entities);
        }

        #endregion

        #region Update entities transactionally (sync & async)

        [Fact, TestPriority(700)]
        public void UpdateMergeEntitiesTransactionallyTest()
        {
            AzTableUnitTestHelper.AssertUpdateOrUpsertExistingEntitiesTransactionally(
                AzTableUnitTestHelper.UpdateMergeEntitiesTransactionally,
                recEntt => {
                    Assert.Equal(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(1)), AzTableUnitTestHelper.GenerateTestPropValue(1));
                    Assert.Equal(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(2)), AzTableUnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact, TestPriority(700)]
        public void UpdateReplaceEntitiesTransactionallyTest()
        {
            AzTableUnitTestHelper.AssertUpdateOrUpsertExistingEntitiesTransactionally(
                AzTableUnitTestHelper.UpdateReplaceEntitiesTransactionally,
                recEntt => {
                    Assert.Null(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(1)));
                    Assert.Equal(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(2)), AzTableUnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact, TestPriority(700)]
        public void UpdateMergeEntitiesTransactionallyAsyncTest()
        {
            AzTableUnitTestHelper.AssertUpdateOrUpsertExistingEntitiesTransactionally(
                AzTableUnitTestHelper.UpdateMergeEntitiesTransactionallyAsync,
                recEntt => {
                    Assert.Equal(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(1)), AzTableUnitTestHelper.GenerateTestPropValue(1));
                    Assert.Equal(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(2)), AzTableUnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact, TestPriority(700)]
        public void UpdateReplaceEntitiesTransactionallyAsyncTest()
        {
            AzTableUnitTestHelper.AssertUpdateOrUpsertExistingEntitiesTransactionally(
                AzTableUnitTestHelper.UpdateReplaceEntitiesTransactionallyAsync,
                recEntt => {
                    Assert.Null(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(1)));
                    Assert.Equal(recEntt.GetString(AzTableUnitTestHelper.GenerateTestPropKey(2)), AzTableUnitTestHelper.GenerateTestPropValue(2));
                });
        }

        #endregion

        #region Upsert entities transactionally (sync & async)

        [Fact, TestPriority(700)]
        public void UpsertNewEntitiesTransactionallyTest()
        {
            // Arrange
            var entitiesA = AzTableUnitTestHelper.CreateSomeEntities(UnitTestHelper.GetUnderOneHundredRandomValue());
            var entitiesB = AzTableUnitTestHelper.CreateSomeEntities(UnitTestHelper.GetUnderOneHundredRandomValue());

            // Act
            var _updateMergeEntitiesResponseTransactionally = AzTableUnitTestHelper.UpsertMergeEntitiesTransactionally(entitiesA);
            var _updateReplaceEntitiesResponseTransactionally = AzTableUnitTestHelper.UpsertReplaceEntitiesTransactionally(entitiesB);

            // Assert
            AzTableUnitTestHelper.AssertSucceededResponses(_updateMergeEntitiesResponseTransactionally);
            //AzTableUnitTestHelper.AssertByGetEntity(entitiesA);

            AzTableUnitTestHelper.AssertSucceededResponses(_updateReplaceEntitiesResponseTransactionally);
            //AzTableUnitTestHelper.AssertByGetEntity(entitiesB);
        }

        [Fact, TestPriority(700)]
        public void UpsertNewEntitiesTransactionallyAsyncTest()
        {
            // Arrange
            var entitiesA = AzTableUnitTestHelper.CreateSomeEntities(UnitTestHelper.GetUnderOneHundredRandomValue());
            var entitiesB = AzTableUnitTestHelper.CreateSomeEntities(UnitTestHelper.GetUnderOneHundredRandomValue());

            // Act
            var _updateMergeEntitiesResponseTransactionally = AzTableUnitTestHelper.UpsertMergeEntitiesTransactionallyAsync(entitiesA);
            var _updateReplaceEntitiesResponseTransactionally = AzTableUnitTestHelper.UpsertReplaceEntitiesTransactionallyAsync(entitiesB);

            // Assert
            AzTableUnitTestHelper.AssertSucceededResponses(_updateMergeEntitiesResponseTransactionally);
            //AzTableUnitTestHelper.AssertByGetEntity(entitiesA);

            AzTableUnitTestHelper.AssertSucceededResponses(_updateReplaceEntitiesResponseTransactionally);
            //AzTableUnitTestHelper.AssertByGetEntity(entitiesB);
        }

        #endregion

        #region Delete entities transactionally (sync & async)

        [Fact, TestPriority(700)]
        public void DeleteEntitiesTransactionallyTest()
        {
            // Arrange
            var entities = AzTableUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.RandomMinValue, ConstProvider.RandomMaxValue);

            // Act
            var _deleteEntitiesTransactionallyResponsesAct = AzTableUnitTestHelper.DeleteEntitiesTransactionally(entities);

            // Assert
            AzTableUnitTestHelper.AssertSucceededResponses(_deleteEntitiesTransactionallyResponsesAct);

            //AzTableUnitTestHelper.AssertByExpectedFailedGetEntity(entities, ConstProvider.The_specified_resource_does_not_exist);
        }

        [Fact, TestPriority(700)]
        public void DeleteEntitiesTransactionallyAsyncTest()
        {
            // Arrange
            var entities = AzTableUnitTestHelper.CreateAddAssertSomeEntities(true,
                ConstProvider.RandomMinValue, ConstProvider.RandomMaxValue);

            // Act
            var _deleteEntitiesTransactionallyAsyncResponsesAct = Helper.RunAsSync(entities, AzTableUnitTestHelper.DeleteEntitiesTransactionallyAsync);

            // Assert
            AzTableUnitTestHelper.AssertSucceededResponses(_deleteEntitiesTransactionallyAsyncResponsesAct);

            //AzTableUnitTestHelper.AssertByExpectedFailedGetEntity(entities, ConstProvider.The_specified_resource_does_not_exist);
        }

        #endregion
    }
}
