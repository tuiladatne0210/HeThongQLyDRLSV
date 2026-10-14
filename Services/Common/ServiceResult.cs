namespace EMS_MVC.Services.Common;

/// <summary>
/// Kết quả trả về của các thao tác trong Service Layer
/// </summary>
public class ServiceResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public static ServiceResult Ok(string message = "Thao tác thành công.") =>
        new() { Success = true, Message = message };

    public static ServiceResult Failed(string error) =>
        new() { Success = false, Message = error };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; set; }

    public static ServiceResult<T> Ok(T data, string message = "Thao tác thành công.") =>
        new() { Success = true, Message = message, Data = data };

    public new static ServiceResult<T> Failed(string error) =>
        new() { Success = false, Message = error };
}
