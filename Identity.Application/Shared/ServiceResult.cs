using MyApp.Application.Shared;

public class ServiceResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public BusinessErrorType ErrorType { get; set; }  // Your enum value
    public string Message { get; set; } = string.Empty;  // Initialize as empty

    // Success factory methods
    public static ServiceResult<T> Success(T data, string message = "") => new()
    {
        IsSuccess = true,
        Data = data,
        Message = message,
        ErrorType = BusinessErrorType.None
    };

    // Failure factory methods
    public static ServiceResult<T> Failure(BusinessErrorType errorType, string message = "") => new()
    {
        IsSuccess = false,
        ErrorType = errorType,
        Message = message
    };
}

