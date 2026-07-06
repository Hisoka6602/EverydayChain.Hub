namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class ApiResponse<T> {
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public static ApiResponse<T> Success(T data, string message) {
        // 步骤：按既定流程执行当前方法逻辑。
        return new ApiResponse<T> {
            IsSuccess = true,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public static ApiResponse<T> Fail(string message) {
        // 步骤：按既定流程执行当前方法逻辑。
        return new ApiResponse<T> {
            IsSuccess = false,
            Message = message,
            Data = default
        };
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public static ApiResponse<T> Fail(string message, T data) {
        // 步骤：按既定流程执行当前方法逻辑。
        return new ApiResponse<T> {
            IsSuccess = false,
            Message = message,
            Data = data
        };
    }
}

