namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 统一 API 返回包装。
/// </summary>
/// <typeparam name="T">业务数据类型。</typeparam>
public sealed class ApiResponse<T> {
    /// <summary>
    /// 接口处理是否成功。
    /// true 表示业务处理完成且可消费 <see cref="Data"/>；
    /// false 表示处理失败，失败原因见 <see cref="Message"/>。
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 响应消息文本。
    /// 成功时通常为业务成功提示；失败时为可读错误原因。
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 业务数据载荷。
    /// 失败场景可能为 null；部分失败场景可能返回附带数据用于定位问题。
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

    /// <summary>
    /// 生成包含业务数据的失败响应。
    /// </summary>
    /// <param name="message">失败消息。</param>
    /// <param name="data">失败附带数据。</param>
    /// <returns>失败响应。</returns>
    public static ApiResponse<T> Fail(string message, T data) {
        return new ApiResponse<T> {
            IsSuccess = false,
            Message = message,
            Data = data
        };
    }
}
