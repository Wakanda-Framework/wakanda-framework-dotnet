using System.Net;
using org.wakanda.framework.Enums;

namespace org.wakanda.framework.Exceptions
{
    public class PreProcessingException : BaseException
    {
        public PreProcessingException(HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
            : base(ErrorType.PRE_PROCESSING_ERROR, httpStatusCode)
        { }

        public PreProcessingException(string message, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
            : base(ErrorType.PRE_PROCESSING_ERROR, message, httpStatusCode)
        { }

        public PreProcessingException(ErrorType errorType, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
            : base(errorType, httpStatusCode)
        { }

        public PreProcessingException(ErrorType errorType, string message, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
            : base(errorType, message, httpStatusCode)
        { }
    }
}