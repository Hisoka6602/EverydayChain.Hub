namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 统一 API 返回包装。
/// </summary>
/// <typeparam name="T">业务数据类型。</typeparam>
public sealed class ApiResponse<T> {
    /// <summary>
    /// 是否成功。
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 响应消息。
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 业务数据。
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// 生成成功响应。
    /// </summary>
    /// <param name="data">业务数据。</param>
    /// <param name="message">响应消息。</param>
    /// <returns>成功响应。</returns>
    public static ApiResponse<T> Success(T data, string message) {
        return new ApiResponse<T> {
            IsSuccess = true,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    /// 生成失败响应。
    /// </summary>
    /// <param name="message">失败消息。</param>
    /// <returns>失败响应。</returns>
    public static ApiResponse<T> Fail(string message) {
        return new ApiResponse<T> {
            IsSuccess = false,
            Message = message,
            Data = default
        };
    }
}
