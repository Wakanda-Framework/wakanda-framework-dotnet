using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using org.wakanda.framework.Models;
using System.Net;

namespace org.wakanda.framework.Helpers
{
    public static class ResponseHelper
    {
        public static IActionResult DefaultOk(string? message = null)
        {
            return new OkObjectResult(
                new ApiResponse<string>(
                    true, 
                    null, 
                    message ?? ResponseType.OK.Message, 
                    ResponseType.OK.Code));
        }

        public static IActionResult Ok<T>(T data, string? message = null)
        {
            return new OkObjectResult(
                new ApiResponse<T>(
                    true, 
                    data, 
                    message ?? ResponseType.OK.Message, 
                    ResponseType.OK.Code));
        }

        public static IActionResult Created<T>(
            string actionName, 
            object routeValues, 
            T data, 
            string? message = null)
        {
            var response = new ApiResponse<T>(
                true, 
                data, 
                message ?? ResponseType.CREATED.Message, 
                ResponseType.CREATED.Code);
            return new CreatedAtActionResult(
                actionName, 
                null, 
                routeValues, 
                response);
        }

        public static IActionResult Error<T>(
            HttpStatusCode status, 
            ResponseType responseType, 
            string? message = null)
        {
            return new ObjectResult(
                new ApiResponse<T>(
                    false, 
                    default, 
                    message ?? responseType.Message, 
                    responseType.Code))
            {
                StatusCode = (int)status
            };
        }
    }
}