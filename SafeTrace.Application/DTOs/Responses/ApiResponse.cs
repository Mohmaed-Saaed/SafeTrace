namespace SafeTrace.Application.DTOs.Responses
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public int StatusCode { get; set; }
        public T? Data { get; set; }

        public static ApiResponse<T> Ok(T? data = default, string message = "Success", int statusCode = 200)
            => new() { Success = true, Message = message, Data = data, StatusCode = statusCode };

        public static ApiResponse<T> Fail(string message = "Failed", int statusCode = 400)
            => new() { Success = false, Message = message, StatusCode = statusCode };
    }
}