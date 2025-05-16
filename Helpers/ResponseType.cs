namespace org.wakanda.framework.Helpers
{
    public record ResponseType(string Code, string Message)
    {
        public static readonly ResponseType OK = new("200", "Success");
        public static readonly ResponseType CREATED = new("201", "Created");
        public static readonly ResponseType ACCEPTED = new("202", "Accepted");
        public static readonly ResponseType NO_CONTENT = new("204", "No Content");
        public static readonly ResponseType BAD_REQUEST = new("400", "Bad Request");
        public static readonly ResponseType NOT_FOUND = new("404", "Not Found");
        public static readonly ResponseType SERVER_ERROR = new("500", "Server Error");
        public static readonly ResponseType UNKNOWN_ERROR = new("999", "Unknown Error");
    }
}