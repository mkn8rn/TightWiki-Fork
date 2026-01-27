namespace TightWiki.Contracts.Responses
{
    /// <summary>
    /// Standard API response wrapper.
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> Errors { get; set; } = [];

        public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };
        public static ApiResponse<T> Fail(string message) => new() { Success = false, ErrorMessage = message };
        public static ApiResponse<T> Fail(List<string> errors) => new() { Success = false, Errors = errors };
    }

    /// <summary>
    /// Non-generic version for operations without return data.
    /// </summary>
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> Errors { get; set; } = [];

        public static ApiResponse Ok() => new() { Success = true };
        public static ApiResponse Fail(string message) => new() { Success = false, ErrorMessage = message };
    }
}
