using System;
using System.Net;
using org.wakanda.framework.Enums;
using Microsoft.Extensions.Localization;

namespace org.wakanda.framework.Exceptions
{
    public class BaseException : Exception
    {
        public int ErrorCode { get; }
        public ErrorType ErrorType { get; }
        public HttpStatusCode HttpStatusCode { get; }

        public BaseException(ErrorType errorType, HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError)
            : base(GetDefaultMessage(errorType))
        {
            ErrorCode = (int)errorType;
            ErrorType = errorType;
            HttpStatusCode = httpStatusCode;
        }

        public BaseException(ErrorType errorType, string message, HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError)
            : base(message)
        {
            ErrorCode = (int)errorType;
            ErrorType = errorType;
            HttpStatusCode = httpStatusCode;
        }

        public BaseException(int errorCode, string message, HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError)
            : base(message)
        {
            ErrorCode = errorCode;
            ErrorType = (ErrorType)errorCode;
            HttpStatusCode = httpStatusCode;
        }

        private static string GetDefaultMessage(ErrorType errorType)
        {
            return errorType switch
            {
                ErrorType.PRE_PROCESSING_ERROR => "A pre-processing error occurred",
                ErrorType.POST_PROCESSING_ERROR => "A post-processing error occurred",
                ErrorType.NO_DATA_FOUND => "No data found",
                ErrorType.EMPTY_ENTITY => "Entity cannot be null",
                ErrorType.INVALID_MODIFIEDBY => "Modified by field is invalid",
                ErrorType.MISMATCH_IDENTIFIER => "Identifier mismatch",
                ErrorType.MISMATCH_ENTITY => "Entity mismatch",
                ErrorType.UPDATE_FAILED => "Update operation failed",
                ErrorType.DELETE_FAILED => "Delete operation failed",
                _ => "An error occurred"
            };
        }
    }
}