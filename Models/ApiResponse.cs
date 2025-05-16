namespace org.wakanda.framework.Models
{
    public class ApiResponse<T>
    {
        public bool IsSuccessful { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public string Code { get; set; }

        public ApiResponse(bool isSuccess, T? data, string message = "", string code = "200")
        {
            IsSuccessful = isSuccess;
            Data = data;
            Message = message;
            Code = code;
        }
    }
}
