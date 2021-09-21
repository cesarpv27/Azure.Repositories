using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
using CoreTools.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace AzStorage.Test.Samples.Samples_AzQueueRepository
{
    [TestCaseOrderer("AzStorage.Test.Utilities.PriorityOrderer", "AzStorage.Test")]
    public class Sample0_Miscellaneous
    {
        [Fact, TestPriority(50)]
        public void CreateAzQueueRepositoryTest1()
        {
            Assert.Throws<ArgumentNullException>(() => new Repositories.AzQueueRepository(connectionString: string.Empty));
        }

        [Fact, TestPriority(50)]
        public void CreateQueueTest()
        {
            // Arrange
            string messageContent = "create queue test";
            string queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();

            // Arr
            var _sendMessageResponseAct = AzQueueUnitTestHelper.SendMessage(messageContent, queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_sendMessageResponseAct);
        }

        [Fact, TestPriority(50)]
        public void CreateQueueTest2()
        {
            // Arrange
            string messageContent = "create queue test";

            // Arr
            Assert.Throws<ArgumentNullException>("DefaultQueueName", 
                () => AzQueueUnitTestHelper.SendMessage(messageContent, null));

            // Assert
        }

        [Fact, TestPriority(50)]
        public void CreateQueueTest3()
        {
            // Arrange
            string messageContent = null;
            string queueName = AzQueueUnitTestHelper.GetDefaultQueueName;

            // Arr
            Assert.Throws<ArgumentNullException>("messageContent", 
                () => AzQueueUnitTestHelper.SendMessage(messageContent, queueName));

            // Assert
        }

        #region GetAccountName

        [Fact, TestPriority(50)]
        public void GetAccountNameTest()
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();
            var _createQueueIfNotExistsResponseArr = AzQueueUnitTestHelper.CreateQueueIfNotExists(queueName);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_createQueueIfNotExistsResponseArr);

            // Arr
            var _getAccountNameResponseAct = AzQueueUnitTestHelper.GetAccountName(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_getAccountNameResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        #endregion
        
        #region GetMaxPeekableMessages

        [Fact, TestPriority(50)]
        public void GetMaxPeekableMessagesTest()
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();
            var _createQueueIfNotExistsResponseArr = AzQueueUnitTestHelper.CreateQueueIfNotExists(queueName);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_createQueueIfNotExistsResponseArr);

            // Arr
            var _getMaxPeekableMessagesResponseAct = AzQueueUnitTestHelper.GetMaxPeekableMessages(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_getMaxPeekableMessagesResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        #endregion

        #region GetMessageMaxBytes

        [Fact, TestPriority(50)]
        public void GetMessageMaxBytesTest()
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();
            var _createQueueIfNotExistsResponseArr = AzQueueUnitTestHelper.CreateQueueIfNotExists(queueName);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_createQueueIfNotExistsResponseArr);

            // Arr
            var _getMessageMaxBytesResponseAct = AzQueueUnitTestHelper.GetMessageMaxBytes(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_getMessageMaxBytesResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        #endregion

        #region GetProperties (sync & async)

        [Fact, TestPriority(50)]
        public void GetPropertiesTest()
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();
            var _createQueueIfNotExistsResponseArr = AzQueueUnitTestHelper.CreateQueueIfNotExists(queueName);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_createQueueIfNotExistsResponseArr);

            // Arr
            var _getPropertiesResponseAct = AzQueueUnitTestHelper.GetProperties(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_getPropertiesResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(50)]
        public void GetPropertiesAsyncTest()
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();
            var _createQueueIfNotExistsResponseArr = AzQueueUnitTestHelper.CreateQueueIfNotExists(queueName);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_createQueueIfNotExistsResponseArr);

            // Arr
            var _getPropertiesResponseAct = AzQueueUnitTestHelper.GetPropertiesAsync(queueName)
                .WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulGenResponse(_getPropertiesResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        #endregion

        #region Delete queue

        [Fact, TestPriority(50)]
        public void DeleteQueueIfExistsTest()
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();
            var _createQueueIfNotExistsResponseArr = AzQueueUnitTestHelper.CreateQueueIfNotExists(queueName);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_createQueueIfNotExistsResponseArr);

            // Arr
            var _deleteQueueIfExistsResponseAct = AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_deleteQueueIfExistsResponseAct);
        }
        
        [Fact, TestPriority(50)]
        public void DeleteQueueIfExistsTest2()// Queue does not exist
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();

            // Arr
            var _deleteQueueIfExistsResponseAct = AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);

            // Assert
            Assert.False(_deleteQueueIfExistsResponseAct.Succeeded);
        }

        #endregion

        #region Exists queue (sync & async)

        [Fact, TestPriority(50)]
        public void ExistsTest()
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();
            var _createQueueIfNotExistsResponseArr = AzQueueUnitTestHelper.CreateQueueIfNotExists(queueName);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_createQueueIfNotExistsResponseArr);

            // Arr
            var _existsResponseAct = AzQueueUnitTestHelper.Exists(queueName);

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_existsResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(50)]
        public void ExistsTest2()// Queue does not exist
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();

            // Arr
            var _existsResponseAct = AzQueueUnitTestHelper.Exists(queueName);

            // Assert
            Assert.False(_existsResponseAct.Succeeded);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }
        
        [Fact, TestPriority(50)]
        public void ExistsAsyncTest()
        {
            // Arrange
            var queueName = AzQueueUnitTestHelper.GetRandomQueueNameFromDefault();
            var _createQueueIfNotExistsResponseArr = AzQueueUnitTestHelper.CreateQueueIfNotExists(queueName);
            UnitTestHelper.AssertExpectedSuccessfulResponse(_createQueueIfNotExistsResponseArr);

            // Arr
            var _existsResponseAct = AzQueueUnitTestHelper.ExistsAsync(queueName).WaitAndUnwrapException();

            // Assert
            UnitTestHelper.AssertExpectedSuccessfulResponse(_existsResponseAct);

            AzQueueUnitTestHelper.DeleteQueueIfExists(queueName);
        }

        #endregion
    }
}
