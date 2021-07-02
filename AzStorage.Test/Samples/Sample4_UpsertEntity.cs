using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using AzStorage.Test.Helpers;
using Azure.Data.Tables;
using AzCoreTools.Core;

namespace AzStorage.Test.Samples
{
    public class Sample4_UpsertEntity
    {
        #region Existing entities

        [Fact]
        public void UpsertMergeExistingEntityTest()
        {
            UnitTestHelper.AssertUpdateOrUpsertExistingEntity(
                UnitTestHelper.UpsertMergeEntity,
                recEntt => {
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(1)), UnitTestHelper.GenerateTestPropValue(1));
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(2)), UnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact]
        public void UpsertReplaceExistingEntityTest()
        {
            UnitTestHelper.AssertUpdateOrUpsertExistingEntity(
                UnitTestHelper.UpsertReplaceEntity,
                recEntt => {
                    Assert.Null(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(1)));
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(2)), UnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact]
        public void UpsertMergeExistingEntitiesTransactionallyTest()
        {
            UnitTestHelper.AssertUpdateOrUpsertExistingEntitiesTransactionally(
                UnitTestHelper.UpsertMergeEntitiesTransactionally,
                recEntt => {
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(1)), UnitTestHelper.GenerateTestPropValue(1));
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(2)), UnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact]
        public void UpsertReplaceExistingEntitiesTransactionallyTest()
        {
            UnitTestHelper.AssertUpdateOrUpsertExistingEntitiesTransactionally(
                UnitTestHelper.UpsertReplaceEntitiesTransactionally,
                recEntt => {
                    Assert.Null(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(1)));
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(2)), UnitTestHelper.GenerateTestPropValue(2));
                });
        }

        #endregion

        #region Existing entities async

        [Fact]
        public void UpsertMergeExistingEntityAsyncTest()
        {
            UnitTestHelper.AssertUpdateOrUpsertExistingEntity(
                UnitTestHelper.UpsertMergeEntityAsync,
                recEntt => {
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(1)), UnitTestHelper.GenerateTestPropValue(1));
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(2)), UnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact]
        public void UpsertReplaceExistingEntityAsyncTest()
        {
            UnitTestHelper.AssertUpdateOrUpsertExistingEntity(
                UnitTestHelper.UpsertReplaceEntityAsync,
                recEntt => {
                    Assert.Null(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(1)));
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(2)), UnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact]
        public void UpsertMergeExistingEntitiesTransactionallyAsyncTest()
        {
            UnitTestHelper.AssertUpdateOrUpsertExistingEntitiesTransactionally(
                UnitTestHelper.UpsertMergeEntitiesTransactionallyAsync,
                recEntt => {
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(1)), UnitTestHelper.GenerateTestPropValue(1));
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(2)), UnitTestHelper.GenerateTestPropValue(2));
                });
        }

        [Fact]
        public void UpsertReplaceExistingEntitiesTransactionallyAsyncTest()
        {
            UnitTestHelper.AssertUpdateOrUpsertExistingEntitiesTransactionally(
                UnitTestHelper.UpsertReplaceEntitiesTransactionallyAsync,
                recEntt => {
                    Assert.Null(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(1)));
                    Assert.Equal(recEntt.GetString(UnitTestHelper.GenerateTestPropKey(2)), UnitTestHelper.GenerateTestPropValue(2));
                });
        }

        #endregion

        #region New entities

        [Fact]
        public void UpsertNewEntityTest()
        {
            // Arrange
            var entityA = UnitTestHelper.CreateSomeEntity();
            var entityB = UnitTestHelper.CreateSomeEntity();

            // Act
            var _upsertMergeEntityResponseAct = UnitTestHelper.UpsertMergeEntity(entityA);
            var _upsertReplaceEntityResponseAct = UnitTestHelper.UpsertReplaceEntity(entityB);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_upsertMergeEntityResponseAct);
            UnitTestHelper.AssertByGetEntity(entityA);

            UnitTestHelper.AssertExpectedSuccessfulResponse(_upsertReplaceEntityResponseAct);
            UnitTestHelper.AssertByGetEntity(entityB);
        }

        [Fact]
        public void UpsertNewEntitiesTransactionallyTest()
        {
            // Arrange
            var entitiesA = UnitTestHelper.CreateSomeEntities(UnitTestHelper.GetUnderOneHundredRandomValue());
            var entitiesB = UnitTestHelper.CreateSomeEntities(UnitTestHelper.GetUnderOneHundredRandomValue());

            // Act
            var _updateMergeEntitiesResponseTransactionally = UnitTestHelper.UpsertMergeEntitiesTransactionally(entitiesA);
            var _updateReplaceEntitiesResponseTransactionally = UnitTestHelper.UpsertReplaceEntitiesTransactionally(entitiesB);

            // Assert
            UnitTestHelper.AssertSucceededResponses(_updateMergeEntitiesResponseTransactionally);
            UnitTestHelper.AssertByGetEntity(entitiesA);

            UnitTestHelper.AssertSucceededResponses(_updateReplaceEntitiesResponseTransactionally);
            UnitTestHelper.AssertByGetEntity(entitiesB);
        }

        #endregion

        #region New entities async

        [Fact]
        public void UpsertNewEntityAsyncTest()
        {
            // Arrange
            var entityA = UnitTestHelper.CreateSomeEntity();
            var entityB = UnitTestHelper.CreateSomeEntity();

            // Act
            var _upsertMergeEntityResponseAct = UnitTestHelper.UpsertMergeEntityAsync(entityA);
            var _upsertReplaceEntityResponseAct = UnitTestHelper.UpsertReplaceEntityAsync(entityB);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_upsertMergeEntityResponseAct);
            UnitTestHelper.AssertByGetEntity(entityA);

            UnitTestHelper.AssertExpectedSuccessfulResponse(_upsertReplaceEntityResponseAct);
            UnitTestHelper.AssertByGetEntity(entityB);
        }

        [Fact]
        public void UpsertNewEntitiesTransactionallyAsyncTest()
        {
            // Arrange
            var entitiesA = UnitTestHelper.CreateSomeEntities(UnitTestHelper.GetUnderOneHundredRandomValue());
            var entitiesB = UnitTestHelper.CreateSomeEntities(UnitTestHelper.GetUnderOneHundredRandomValue());

            // Act
            var _updateMergeEntitiesResponseTransactionally = UnitTestHelper.UpsertMergeEntitiesTransactionallyAsync(entitiesA);
            var _updateReplaceEntitiesResponseTransactionally = UnitTestHelper.UpsertReplaceEntitiesTransactionallyAsync(entitiesB);

            // Assert
            UnitTestHelper.AssertSucceededResponses(_updateMergeEntitiesResponseTransactionally);
            UnitTestHelper.AssertByGetEntity(entitiesA);

            UnitTestHelper.AssertSucceededResponses(_updateReplaceEntitiesResponseTransactionally);
            UnitTestHelper.AssertByGetEntity(entitiesB);
        }

        #endregion
    }
}
