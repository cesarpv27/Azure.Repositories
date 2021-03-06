using System;
using System.Collections.Generic;
using System.Linq;
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

        public static void AssertExpectedSuccessfulResponses(IEnumerable<IAzDetailedResponse> responses)
        {
            foreach (var _resp in responses)
                AssertExpectedSuccessfulResponse(_resp);
        }

        public static void AssertExpectedSuccessfulGenResponses<T>(IEnumerable<IAzDetailedResponse<T>> responses)
        {
            foreach (var _resp in responses)
                AssertExpectedSuccessfulGenResponse(_resp);
        }

        public static void AssertExpectedCancelledGenResponses<T>(
            IEnumerable<IAzDetailedResponse<T>> responses,
            int expectedResponsesAmount = -1)
        {
            AssertExpectedCancelledResponses(responses, expectedResponsesAmount);
        }

        public static void AssertExpectedCancelledResponses(
            IEnumerable<IAzDetailedResponse> responses,
            int expectedResponsesAmount = -1)
        {
            bool cancellationStarted = false;
            foreach (var _response in responses)
            {
                if (cancellationStarted)
                    Assert.False(_response.Succeeded);

                if (!cancellationStarted && !_response.Succeeded)
                    cancellationStarted = true;
            }

            if (expectedResponsesAmount > 0)
                Assert.Equal(expectedResponsesAmount, responses.Count());
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

        public static string BuildRandomName(string baseName, int randomValue = -1)
        {
            int id = randomValue;
            if (randomValue < 0)
            {
                var rdm = new Random();
                id = rdm.Next(1, int.MaxValue);
            }
            return baseName + id;
        }
    }
}
