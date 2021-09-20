using System;
using System.Collections.Generic;
using System.Text;
using AzCoreTools.Core.Interfaces;
using Xunit;

namespace AzStorage.Test.Helpers
{
    internal class UnitTestHelper
    {
        #region Assert successful response

        public static void AssertExpectedSuccessfulResponse(IAzDetailedResponse response)
        {
            Assert.True(response.Succeeded);

            Assert.Null(response.Exception);
            Assert.Null(response.Message);
        }

        public static void AssertExpectedSuccessfulGenResponse<T>(IAzDetailedResponse<T> response)
        {
            AssertExpectedSuccessfulResponse(response);

            Assert.NotNull(response.Value);
        }

        #endregion

        #region Assert failed response

        public static void AssertExpectedFailedResponse(IAzDetailedResponse failedResponse, string errorMessage)
        {
            Assert.False(failedResponse.Succeeded);

            Assert.NotNull(failedResponse.Message);
            Assert.Contains(errorMessage, failedResponse.Message);
        }

        public static void AssertExpectedFailedResponseWithException(IAzDetailedResponse failedResponse, string errorMessage)
        {
            Assert.NotNull(failedResponse.Exception);

            AssertExpectedFailedResponse(failedResponse, errorMessage);
        }
        
        public static void AssertExpectedFailedGenResponse<T>(
            IAzDetailedResponse<T> failedResponse,
            string errorMessage,
            bool assertValue = true)
        {
            AssertExpectedFailedResponse(failedResponse, errorMessage);

            if (assertValue)
                Assert.Null(failedResponse.Value);
        }
        
        public static void AssertExpectedFailedGenResponseWithException<T>(
            IAzDetailedResponse<T> failedResponse,
            string errorMessage,
            bool assertValue = true)
        {
            AssertExpectedFailedResponseWithException(failedResponse, errorMessage);

            if (assertValue)
                Assert.Null(failedResponse.Value);
        }

        #endregion


        public static int GetUnderOneHundredRandomValue()
        {
            return new Random().Next(Utilities.ConstProvider.RandomMinValue,
                Utilities.ConstProvider.RandomMaxValue);
        }

        public static int GetOverOneHundredRandomValue()
        {
            return new Random().Next(Utilities.ConstProvider.Hundreds_RandomMinValue,
                Utilities.ConstProvider.Hundreds_RandomMaxValue);
        }

    }
}
