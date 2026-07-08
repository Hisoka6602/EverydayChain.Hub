namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示接口统一响应包装，包含是否成功、提示消息和业务数据。
/// </summary>
public sealed class ApiResponse<T> {
    /// <summary>
    /// 表示当前接口调用或回传结果是否处理成功。
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 表示本次接口处理返回的提示信息。
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 表示接口返回的业务数据内容。
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// 创建成功响应，并携带业务数据与提示消息。
    /// </summary>
    public static ApiResponse<T> Success(T data, string message) {
        // 步骤：构造成功标记、提示消息和业务数据，返回统一响应对象。
        return new ApiResponse<T> {
            IsSuccess = true,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    /// 创建失败响应，并返回失败提示消息。
    /// </summary>
    public static ApiResponse<T> Fail(string message) {
        // 步骤：构造失败标记和提示消息，返回不包含业务数据的统一响应对象。
        return new ApiResponse<T> {
            IsSuccess = false,
            Message = message,
            Data = default
        };
    }

    /// <summary>
    /// 创建失败响应，并附带当前仍需回传的业务数据。
    /// </summary>
    public static ApiResponse<T> Fail(string message, T data) {
        // 步骤：构造失败标记、提示消息和业务数据，返回统一响应对象。
        return new ApiResponse<T> {
            IsSuccess = false,
            Message = message,
            Data = data
        };
    }
}

