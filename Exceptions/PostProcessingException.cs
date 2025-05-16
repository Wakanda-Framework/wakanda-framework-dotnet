using System.Net;
using org.wakanda.framework.Enums;

namespace org.wakanda.framework.Exceptions
{
    /// <summary>
    /// Exception thrown when post-processing steps fail
    /// </summary>
    public class PostProcessingException : BaseException
    {
        public PostProcessingException(HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError)
            : base(ErrorType.POST_PROCESSING_ERROR, httpStatusCode)
        { }

        public PostProcessingException(string message, HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError)
            : base(ErrorType.POST_PROCESSING_ERROR, message, httpStatusCode)
        { }

        public PostProcessingException(ErrorType errorType, HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError)
            : base(errorType, httpStatusCode)
        { }

        public PostProcessingException(ErrorType errorType, string message, HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError)
            : base(errorType, message, httpStatusCode)
        { }
    }
}