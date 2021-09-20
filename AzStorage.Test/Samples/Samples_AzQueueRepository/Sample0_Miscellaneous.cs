using AzStorage.Test.Helpers;
using AzStorage.Test.Utilities;
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
            var rdm = new Random();
            string queueName = AzQueueUnitTestHelper.GetDefaultQueueName + rdm.Next(1, int.MaxValue);

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
    }
}
